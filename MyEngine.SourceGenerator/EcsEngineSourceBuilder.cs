using System.Collections.Generic;
using System.Linq;

namespace MyEngine.SourceGenerator
{
    public class EcsEngineSourceBuilder
    {
        public static (string FileName, string Contents) BuildEcsEngineSource(
            IReadOnlyCollection<StartupSystemClass> startupSystemClassModels,
            IReadOnlyCollection<SystemClass> systemClassModels,
            string appEntrypointFullyQualifiedName)
        {
var ecsEngineTemplate = SourceTemplate.LoadFromEmbeddedResource("EcsEngine.template");


                var startupSystemInstantiations = startupSystemClassModels.Select(BuildStartupSystemInstantiation);
            var systemInstantiations = systemClassModels.Select(BuildSystemInstantiation);

                ecsEngineTemplate.SubstitutePart("AppEntrypointName", appEntrypointFullyQualifiedName);
                ecsEngineTemplate.SubstitutePart("StartupSystemInstantiations", string.Join("\r\n\r\n", startupSystemInstantiations));
                ecsEngineTemplate.SubstitutePart("SystemInstantiations", string.Join("\r\n\r\n", systemInstantiations));
                ecsEngineTemplate.SubstitutePart("AllStartupSystemTypesArray", startupSystemClassModels.Count == 0
                    ? "Array.Empty<Type>()"
                    : $@"new Type[]
{{
    {string.Join(",\r\n    ", startupSystemClassModels.Select(x => $"typeof({x.FullyQualifiedName})"))}
}}");
                ecsEngineTemplate.SubstitutePart("AllSystemTypesArray", systemClassModels.Count == 0
                    ? "Array.Empty<Type>()"
                    : $@"new Type[]
{{
    {string.Join(",\r\n    ", systemClassModels.Select(x => $"typeof({x.FullyQualifiedName})"))}
}}");
                ecsEngineTemplate.SubstitutePart("UninstantiatedStartupSystemsDictionary",
                    startupSystemClassModels.Count == 0
                    ? "new ()"
                    : $@"new ()
{{
    {string.Join(",\r\n    ", startupSystemClassModels.Select(x => $"{{ typeof({x.FullyQualifiedName}), {(x.Constructor.Parameters.Count == 0 ? "Array.Empty<Type>()" : $"new Type[] {{ {string.Join(", ", x.Constructor.Parameters.Select(y => $"typeof({y.Name})"))} }}")} }}"))}
}}");
                ecsEngineTemplate.SubstitutePart("UninstantiatedSystemsDictionary",
                    systemClassModels.Count == 0
                    ? "new ()"
                    : $@"new ()
{{
    {string.Join(",\r\n    ", systemClassModels.Select(x => $"{{ typeof({x.FullyQualifiedName}), {(x.Constructor.Parameters.Count == 0 ? "Array.Empty<Type>()" : $"new Type[] {{ {string.Join(", ", x.Constructor.Parameters.Where(y => y.IsResource).Select(y => $"typeof({y.Name})"))} }}")} }}"))}
}}");

                var ecsEngineBody = ecsEngineTemplate.Build();

                return ("EcsEngine.g.cs", ecsEngineBody);
        }

        private static string BuildStartupSystemInstantiation(StartupSystemClass systemClass)
        {
            var template = SourceTemplate.LoadFromEmbeddedResource("EcsEngineStartupSystemInstantiation.template");
            template.SubstitutePart("StartupSystemFullyQualifiedName", systemClass.FullyQualifiedName);

            var resourceChecks = systemClass.Constructor.Parameters
                .Select((x, i) => (Parameter: x, Index: i))
                .Select(x => (ResourceCheck: $"_resourceContainer.TryGetResource<{x.Parameter.Name}>(out var resource{x.Index + 1})", ParameterName: $"resource{x.Index + 1}"))
                .ToArray();

            template.SubstitutePart("ResourceChecks", string.Join("\r\n&& ", resourceChecks.Select(x => x.ResourceCheck)));
            template.SubstitutePart("StartupSystemParameters", string.Join(", ", resourceChecks.Select(x => x.ParameterName)));
            
            return template.Build();
        }

        private static string BuildSystemInstantiation(SystemClass systemClass)
        {
            var sourceTemplate = SourceTemplate.LoadFromEmbeddedResource("EcsEngineSystemInstantiation.template");
            sourceTemplate.SubstitutePart("SystemFullyQualifiedName", systemClass.FullyQualifiedName);

            var getComponentFuncs = systemClass.Constructor.Parameters
                .Where(x => !x.IsResource)
                .Select((x, i) => BuildGetComponentsFunc(x.QueryComponentTypeParameters, i + 1))
                .ToArray();
            sourceTemplate.SubstitutePart("GetQueryComponentFuncs", string.Join("\r\n\r\n", getComponentFuncs));

            var resourceChecks = systemClass.Constructor.Parameters
                .Where(x => x.IsResource)
                .Select((x, i) => (Parameter: x, Index: i))
                .ToDictionary(x => x.Parameter.Name, x => (ResourceCheck: $"_resourceContainer.TryGetResource<{x.Parameter.Name}>(out var resource{x.Index})", ParameterName: $"resource{x.Index}"));

            if (resourceChecks.Count > 0)
            {
                sourceTemplate.SubstitutePart("ResourceChecks", string.Join("\r\n&& ", resourceChecks.Values.Select(x => x.ResourceCheck)));
            }
            else
            {
                sourceTemplate.SubstitutePart("ResourceChecks", "true");
            }

            var queryParameters = systemClass.Constructor.Parameters.Select((x, i) => (Parameter: x, Index: i))
                .Where(x => !x.Parameter.IsResource)
                .Select((x, queryIndex) => (QueryCreation: $"global::MyEngine.Runtime.Query.Create(_components, _entities, GetQuery{queryIndex + 1}Components)", x.Index))
                .ToDictionary(x => x.Index, x => x.QueryCreation);

            var parameters = systemClass.Constructor.Parameters.Select((x, i) => x.IsResource
                ? resourceChecks[x.Name].ParameterName
                : queryParameters[i]);

            sourceTemplate.SubstitutePart("SystemParameters", string.Join(",\r\n", parameters));

            return sourceTemplate.Build();
        }

        private static string BuildGetComponentsFunc(IReadOnlyCollection<QueryComponentTypeParameter> queryParameters, int queryNumber)
        {
            var template = SourceTemplate.LoadFromEmbeddedResource("EcsEngineSystemInstantiationGetComponentFunc.template");
            var entityComponentsTypeArguments = string.Join(",\r\n",
                queryParameters.Select(x => x.MetaComponentType == MetaComponentType.OptionalComponent
                    ? $"MyEngine.Core.Ecs.Components.OptionalComponent<{x.ComponentTypeName}>"
                    : x.ComponentTypeName));

            template.SubstitutePart("QueryTypeParameters", entityComponentsTypeArguments);
            template.SubstitutePart("QueryNumber", queryNumber.ToString());

            var requiredComponentChecks = queryParameters.Select((x, i) => (Parameter: x, Index: i))
                .Where(x => x.Parameter.MetaComponentType is null)
                .Select(x => $"_components.TryGetComponent<{x.Parameter.ComponentTypeName}>(entityId, out var component{x.Index + 1})")
                .ToArray();

            if (requiredComponentChecks.Length > 0)
            {
                template.SubstitutePart("RequiredComponentChecks", string.Join("\r\n&& ", requiredComponentChecks));
            }
            else
            {
                template.SubstitutePart("RequiredComponentChecks", "true");
            }

            var optionalComponentChecks = queryParameters.Select((x, i) => (Parameter: x, Index: i))
                .Where(x => x.Parameter.MetaComponentType == MetaComponentType.OptionalComponent)
                .Select(x => $"var component{x.Index + 1} = _components.GetOptionalComponent<{x.Parameter.ComponentTypeName}>(entityId);");
            template.SubstitutePart("OptionalComponentVariables", string.Join("\r\n", optionalComponentChecks));

            var propertyAssignments = Enumerable.Range(0, queryParameters.Count)
                .Select(x => $"Component{x + 1} = component{x + 1}");

            template.SubstitutePart("EcsComponentsPropertyAssignments", string.Join(",\r\n", propertyAssignments));

            return template.Build();
        }
    }
}
