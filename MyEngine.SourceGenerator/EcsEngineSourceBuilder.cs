using System.Collections.Generic;
using System.Linq;

namespace MyEngine.SourceGenerator
{
    public class EcsEngineSourceBuilder
    {
        public static (string FileName, string Contents) BuildEcsEngineSource(
            IReadOnlyCollection<StartupSystemClassDto> startupSystemClassModels,
            IReadOnlyCollection<SystemClassDto> systemClassModels,
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
    {string.Join(",\r\n    ", systemClassModels.Select(x => $"{{ typeof({x.FullyQualifiedName}), {(x.Constructor.TotalParameters == 0 ? "Array.Empty<Type>()" : $"new Type[] {{ {string.Join(", ", x.Constructor.ResourceParameters.Select(y => $"typeof({y.Name})"))} }}")} }}"))}
}}");

            var ecsEngineBody = ecsEngineTemplate.Build();

            return ("EcsEngine.g.cs", ecsEngineBody);
        }

        public static string BuildStartupSystemInstantiation(StartupSystemClassDto systemClass)
        {
            var template = SourceTemplate.LoadFromEmbeddedResource("EcsEngineStartupSystemInstantiation.template");
            template.SubstitutePart("StartupSystemFullyQualifiedName", systemClass.FullyQualifiedName);

            var resourceChecks = systemClass.Constructor.Parameters
                .Select((x, i) => (Parameter: x, Index: i))
                .Select(x => (ResourceCheck: $"_resourceContainer.TryGetResource<{x.Parameter.Name}>(out var resource{x.Index + 1})", ParameterName: $"resource{x.Index + 1}"))
                .ToArray();

            var resourceChecksJoined = resourceChecks.Length == 0 ? "true" : string.Join("\r\n&& ", resourceChecks.Select(x => x.ResourceCheck));

            template.SubstitutePart("ResourceChecks", resourceChecksJoined);
            template.SubstitutePart("StartupSystemParameters", string.Join(", ", resourceChecks.Select(x => x.ParameterName)));

            return template.Build();
        }

        public static string BuildSystemInstantiation(SystemClassDto systemClass)
        {
            var sourceTemplate = SourceTemplate.LoadFromEmbeddedResource("EcsEngineSystemInstantiation.template");
            sourceTemplate.SubstitutePart("SystemFullyQualifiedName", systemClass.FullyQualifiedName);

            var getComponentFuncs = systemClass.Constructor.QueryParameters
                .Select((x, i) => BuildGetComponentsFunc(x.TypeParameters, i + 1))
                .ToArray();
            sourceTemplate.SubstitutePart("GetQueryComponentFuncs", string.Join("\r\n\r\n", getComponentFuncs));

            var resourceChecks = systemClass.Constructor.ResourceParameters
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

            var parameters = new string[systemClass.Constructor.TotalParameters];
            foreach (var (parameterIndex, queryIndex) in systemClass.Constructor.QueryParameters.Select((x, i) => (x.ParameterIndex, i)))
            {
                parameters[parameterIndex] = $"global::MyEngine.Runtime.Query.Create(_components, _entities, GetQuery{queryIndex + 1}Components)";
            }

            foreach (var (parameterIndex, resourceParameter) in systemClass.Constructor.ResourceParameters.Select(x => (x.ParameterIndex, x.Name)))
            {
                parameters[parameterIndex] = resourceChecks[resourceParameter].ParameterName;
            }

            sourceTemplate.SubstitutePart("SystemParameters", string.Join(",\r\n", parameters));

            return sourceTemplate.Build();
        }

        private static string BuildGetComponentsFunc(IReadOnlyCollection<QueryComponentTypeParameterDto> queryParameters, int queryNumber)
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
