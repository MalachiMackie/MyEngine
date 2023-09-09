using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace MyEngine.SourceGenerator
{
    [Generator]
    public class AppEntrypointInformationRetrievalSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var nodesWithAppEntrypointAttribute = context.SyntaxProvider.ForAttributeWithMetadataName("MyEngine.Core.AppEntrypointAttribute",
                (syntaxNode, _) => true,
                (generatorAttributeSyntaxContext, _) => (generatorAttributeSyntaxContext.SemanticModel, ClassNode: generatorAttributeSyntaxContext.TargetNode as ClassDeclarationSyntax))
                .Where(x => IsClassConcreteAndAccessible(x.SemanticModel, x.ClassNode))
                .Where(x => DoesClassNodeImplementInterface(x.SemanticModel, x.ClassNode, "MyEngine.Core.IAppEntrypoint"))
                .Where(x => DoesEntrypointHaveAccessibleConstructor(x.ClassNode));

            var classNodes = context.SyntaxProvider.CreateSyntaxProvider((x, _) => x is ClassDeclarationSyntax, (x, _) => (x.SemanticModel, ClassNode: x.Node as ClassDeclarationSyntax));
            var accessibleClassNodes = classNodes
                .Where(x => IsClassConcreteAndAccessible(x.SemanticModel, x.ClassNode));

            var startupSystemClasses = accessibleClassNodes
                .Where(x => DoesClassNodeImplementInterface(x.SemanticModel, x.ClassNode, "MyEngine.Core.Ecs.Systems.IStartupSystem"))
                .Select((x, _) => (x.SemanticModel, x.ClassNode, Constructor: TryGetStartupSystemConstructor(x.SemanticModel, x.ClassNode)))
                .Where(x => x.Constructor != null)
                .Select((x, _) => new StartupSystemClass(GetFullyQualifiedName(x.ClassNode), x.Constructor))
                .Collect();

            var systemClasses = accessibleClassNodes
                .Where(x => DoesClassNodeImplementInterface(x.SemanticModel, x.ClassNode, "MyEngine.Core.Ecs.Systems.ISystem"))
                .Select((x, _) => (x.SemanticModel, x.ClassNode, Constructor: TryGetSystemConstructor(x.SemanticModel, x.ClassNode)))
                .Where(x => x.Constructor != null)
                .Select((x, _) => new SystemClass(GetFullyQualifiedName(x.ClassNode), x.Constructor))
                .Collect();

            var compilationValue = context.CompilationProvider.Select((x, _) => x);

            context.RegisterSourceOutput(compilationValue, (sourceProductionContext, compilation) =>
            {
                if (compilation.AssemblyName != "MyEngine.Runtime")
                {
                    return;
                }

                var appSystemsInfoTypes = compilation.References.Select(x => compilation.GetAssemblyOrModuleSymbol(x) as IAssemblySymbol)
                    .SelectMany(x => GetAllNamespaceTypes(x.GlobalNamespace))
                    .Where(x => x.GetAttributes().Any(y => y.AttributeClass != null && y.AttributeClass.ToDisplayString() == "MyEngine.Core.AppSystemsInfoAttribute"))
                    .ToArray();

                var appEntrypointInfo = compilation.GetTypeByMetadataName("MyEngine.Runtime.AppEntrypointInfo");

                var appEntrypointFullyQualifiedName = appEntrypointInfo.GetMembers().FirstOrDefault(x => x.Name == "FullyQualifiedName") as IFieldSymbol;


                var systemClassModels = appSystemsInfoTypes.Select(x => x.GetMembers().FirstOrDefault(y => y.Name == "SystemClasses") as IFieldSymbol)
                    .SelectMany(x => JsonConvert.DeserializeObject<SystemClass[]>((x.ConstantValue as string).Replace("\\\"", "\"")));


                var systemInstantiations = systemClassModels
                    .Select(BuildSystemInstantiation);

                var startupSystemClassModels = appSystemsInfoTypes.Select(x => x.GetMembers().FirstOrDefault(y => y.Name == "StartupSystemClasses") as IFieldSymbol)
                    .SelectMany(x => JsonConvert.DeserializeObject<StartupSystemClass[]>((x.ConstantValue as string).Replace("\\\"", "\"")));

                var startupSystemInstantiations = startupSystemClassModels.Select(BuildStartupSystemInstantiation);


                sourceProductionContext.AddSource("EcsEngine.g.cs", $@"
#nullable enable
namespace MyEngine.Runtime
{{
    internal partial class EcsEngine
    {{
        private static partial global::MyEngine.Core.IAppEntrypoint GetAppEntrypoint() => new {appEntrypointFullyQualifiedName.ConstantValue}();


        private partial void AddStartupSystemInstantiations()
        {{
            {string.Join($"\n\n", startupSystemInstantiations)}
        }}


        private partial void AddSystemInstantiations()
        {{
            {string.Join($"\n\n", systemInstantiations)}
        }}

        private static partial IReadOnlyCollection<Type> GetAllStartupSystemTypes() => new Type[] {{{string.Join(", ", startupSystemClassModels.Select(x => $"typeof({x.FullyQualifiedName})"))}}};
        private static partial IReadOnlyCollection<Type> GetAllSystemTypes() => new Type[] {{{string.Join(", ", systemClassModels.Select(x => $"typeof({x.FullyQualifiedName})"))}}};
        private static partial Dictionary<Type, Type[]> GetUninstantiatedStartupSystems() => new()
        {{
            {string.Join("\n", startupSystemClassModels.Select(x => $"{{ typeof({x.FullyQualifiedName}), new Type[] {{{string.Join(", ", x.Constructor.Parameters.Select(y => $"typeof({y.Name})"))}}}}},"))}
        }};
        private static partial Dictionary<Type, Type[]> GetUninstantiatedSystems() => new()
        {{
            {string.Join("\n", systemClassModels.Select(x => $"{{ typeof({x.FullyQualifiedName}), new Type[] {{{string.Join(", ", x.Constructor.Parameters.Where(y => y.IsResource).Select(y => $"typeof({y.Name})"))}}} }},"))}
        }};
    }}
}}
#nullable restore
");
            });

            var allSystemsAndCompilation = startupSystemClasses.Combine(systemClasses)
                .Combine(compilationValue);

            context.RegisterImplementationSourceOutput(allSystemsAndCompilation, (sourceProductionContext, classesAndCompilation) =>
            {
                var ((startupSystems, systems), compilation) = classesAndCompilation;

                if (compilation.AssemblyName == "MyEngine.Runtime")
                {
                    return;
                }

                var projectNamespace = $"{compilation.AssemblyName}.Generated";

                sourceProductionContext.AddSource("AppSystemsInfo.g.cs", $@"
namespace {projectNamespace}
{{
    [global::MyEngine.Core.AppSystemsInfo]
    public static class AppSystemsInfo
    {{
        public const string SystemClasses = ""{JsonConvert.SerializeObject(systems).Replace("\"", "\\\"")}"";

        public const string StartupSystemClasses = ""{JsonConvert.SerializeObject(startupSystems).Replace("\"", "\\\"")}"";
    }}
}}
");
            });

            context.RegisterSourceOutput(nodesWithAppEntrypointAttribute, (sourceProductionContext, semanticModelAndClassNode) =>
            {
                var classNode = semanticModelAndClassNode.ClassNode;

                var fullyQualifiedName = GetFullyQualifiedName(classNode);
                
                sourceProductionContext.AddSource("AppEntrypointInfo.g.cs", $@"
namespace MyEngine.Runtime
{{
    public static class AppEntrypointInfo
    {{
        public const string FullyQualifiedName = ""{fullyQualifiedName}"";
    }}
}}");
            });
        }

        private IEnumerable<ITypeSymbol> GetAllNamespaceTypes(INamespaceSymbol namespaceSymbol)
        {
            var types = new List<ITypeSymbol>(namespaceSymbol.GetTypeMembers());

            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                types.AddRange(GetAllNamespaceTypes(childNamespace));
            }

            return types;
        }

        private string BuildGetComponentsFunc(IReadOnlyCollection<QueryComponentTypeParameter> queryParameters, int queryNumber)
        {
            var entityComponentsTypeArguments = string.Join(", ", queryParameters.Select(x => x.MetaComponentType == MetaComponentType.OptionalComponent ? $"MyEngine.Core.Ecs.Components.OptionalComponent<{x.ComponentTypeName}>" : x.ComponentTypeName));
            var anyOptional = queryParameters.Any(x => x.MetaComponentType == MetaComponentType.OptionalComponent);

            var requiredComponentChecks = queryParameters.Select((x, i) => (Parameter: x, Index: i))
                .Where(x => x.Parameter.MetaComponentType is null)
                .Select(x => $"_components.TryGetComponent<{x.Parameter.ComponentTypeName}>(entityId, out var component{x.Index + 1})")
                .ToArray();

            var optionalComponentChecks = queryParameters.Select((x, i) => (Parameter: x, Index: i))
                .Where(x => x.Parameter.MetaComponentType == MetaComponentType.OptionalComponent)
                .Select(x => $"var component{x.Index + 1} = _components.GetOptionalComponent<{x.Parameter.ComponentTypeName}>(entityId);");

            var propertyAssignments = Enumerable.Range(0, queryParameters.Count)
                .Select(x => $"Component{x + 1} = component{x + 1},");

            var ifBlock = requiredComponentChecks.Any()
                ? $@"if ({string.Join(" && ", requiredComponentChecks)})
{{" : "";

            var ifBody = $@"{string.Join("\n", optionalComponentChecks)}
return new global::MyEngine.Core.Ecs.Components.EntityComponents<{entityComponentsTypeArguments}>(entityId)
{{
    {string.Join("\n", propertyAssignments)}
}};";

            var endIfBlock = requiredComponentChecks.Any()
                ? @"}
return null;" : "";

            return $@"
global::MyEngine.Core.Ecs.Components.EntityComponents<{entityComponentsTypeArguments}>? GetQuery{queryNumber}Components(global::MyEngine.Core.Ecs.EntityId entityId)
{{
    {ifBlock}
    {ifBody}
    {endIfBlock}
}}";
        }

        private string BuildStartupSystemInstantiation(StartupSystemClass systemClass)
        {
            var resourceChecks = systemClass.Constructor.Parameters
                .Select((x, i) => (Parameter: x, Index: i))
                .Select(x => (ResourceCheck: $"_resourceContainer.TryGetResource<{x.Parameter.Name}>(out var resource{x.Index})", ParameterName: $"resource{x.Index}"))
                .ToArray();

            var resourceCheck = resourceChecks.Length == 0 ? "" : $@"if ({string.Join(" && ", resourceChecks.Select(x => x.ResourceCheck))})
                {{";
            var endResourceCheck = resourceChecks.Length == 0 ? "" : @"}
                return null;";

            var parameters = resourceChecks.Select(x => x.ParameterName);

            var systemInstantiation = $@"return new {systemClass.FullyQualifiedName}({string.Join(", ", parameters)});";

            var addSystemInstantiation = $@"
            _startupSystemInstantiations.Add(typeof({systemClass.FullyQualifiedName}), () =>
            {{
                {resourceCheck}
                    {systemInstantiation}
                {endResourceCheck}
            }});";

            return addSystemInstantiation;

        }


        private string BuildSystemInstantiation(SystemClass systemClass)
        {
            var resourceChecks = systemClass.Constructor.Parameters
                .Where(x => x.IsResource)
                .Select((x, i) => (Parameter: x, Index: i))
                .ToDictionary(x => x.Parameter.Name, x => (ResourceCheck: $"_resourceContainer.TryGetResource<{x.Parameter.Name}>(out var resource{x.Index})", ParameterName: $"resource{x.Index}"));

            var resourceCheck = resourceChecks.Count == 0 ? "" : $@"if ({string.Join(" && ", resourceChecks.Values.Select(x => x.ResourceCheck))})
                {{";
            var endResourceCheck = resourceChecks.Count == 0 ? "" : @"}
                return null;";

            var getComponentFuncs = systemClass.Constructor.Parameters
                .Where(x => !x.IsResource)
                .Select((x, i) => BuildGetComponentsFunc(x.QueryComponentTypeParameters, i))
                .ToArray();

            var queryParameters = systemClass.Constructor.Parameters.Select((x, i) => (Parameter: x, Index: i))
                .Where(x => !x.Parameter.IsResource)
                .Select((x, queryIndex) => ($"global::MyEngine.Runtime.Query.Create(_components, _entities, GetQuery{queryIndex}Components)", x.Index))
                .ToDictionary(x => x.Index, x => x.Item1);

            var parameters = systemClass.Constructor.Parameters.Select((x, i) => x.IsResource
                ? resourceChecks[x.Name].ParameterName
                : queryParameters[i]);

            var systemInstantiation = $@"return new {systemClass.FullyQualifiedName}({string.Join(", ", parameters)});";

            var addSystemInstantiation = $@"
            _systemInstantiations.Add(typeof({systemClass.FullyQualifiedName}), () =>
            {{
                {string.Join("\n\n", getComponentFuncs)}
                {resourceCheck}
                    {systemInstantiation}
                {endResourceCheck}
            }});";

            return addSystemInstantiation;

        }

        private string GetFullyQualifiedName(ClassDeclarationSyntax classNode)
        {
            // todo: figure out if there's a better way to do this
            var className = classNode.Identifier.ToString();
            var classNamespaceNode = classNode.Parent;
            string classNamespace;
            if (classNamespaceNode is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                classNamespace = $"global::{namespaceDeclaration.Name}";
            }
            else if (classNamespaceNode is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
            {
                classNamespace = $"global::{fileScopedNamespaceDeclarationSyntax.Name}";
            }
            else
            {
                // we assumed a class declaration syntax's parent is always a namespace
                throw new Exception($"ClassNode's parent is not a namespace declaration. It was a {classNamespaceNode.GetType().Name}");
            }

            var fullyQualifiedName = classNamespace.Length == 0
                ? className
                : $"{classNamespace}.{className}";

            return fullyQualifiedName;
        }

        private enum MetaComponentType
        {
            OptionalComponent
        }

        private sealed class QueryComponentTypeParameter
        {
            public MetaComponentType? MetaComponentType { get; set; }

            public string ComponentTypeName { get; set; }
        } 

        private sealed class StartupSystemConstructorParameter
        {
            public string Name { get; set; }
        }

        private sealed class SystemConstructorParameter
        {
            public string Name { get; set; }

            public bool IsResource { get; set; }

            public IReadOnlyList<QueryComponentTypeParameter> QueryComponentTypeParameters { get; set; }
        }

        private sealed class StartupSystemConstructor
        {
            public static readonly StartupSystemConstructor NoConstructor = new StartupSystemConstructor(Array.Empty<StartupSystemConstructorParameter>());
            public IReadOnlyCollection<StartupSystemConstructorParameter> Parameters { get; }

            public StartupSystemConstructor(IReadOnlyCollection<StartupSystemConstructorParameter> parameters)
            {
                Parameters = parameters;
            }
        }

        private sealed class SystemConstructor
        {
            public static readonly SystemConstructor NoConstructor = new SystemConstructor(Array.Empty<SystemConstructorParameter>());
            public IReadOnlyCollection<SystemConstructorParameter> Parameters { get; }

            public SystemConstructor(IReadOnlyCollection<SystemConstructorParameter> parameters)
            {
                Parameters = parameters;
            }
        }

        private sealed class StartupSystemClass
        {
            public string FullyQualifiedName { get; }
            public StartupSystemConstructor Constructor { get; }

            public StartupSystemClass(string fullyQualifiedName, StartupSystemConstructor constructor)
            {
                FullyQualifiedName = fullyQualifiedName;
                Constructor = constructor;
            }
        }

        private sealed class SystemClass
        {
            public string FullyQualifiedName { get; }
            public SystemConstructor Constructor { get; }

            public SystemClass(string fullyQualifiedName, SystemConstructor constructor)
            {
                FullyQualifiedName = fullyQualifiedName;
                Constructor = constructor;
            }
        }

        private StartupSystemConstructor TryGetStartupSystemConstructor(SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            var constructorDeclarations = classNode.ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .ToArray();

            // no constructor means public constructor
            if (constructorDeclarations.Length == 0)
            {
                return StartupSystemConstructor.NoConstructor;
            }

            var publicConstructors = constructorDeclarations
                .Where(x => x.ChildTokens().Any(y => y.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToArray();

            // todo: add analyzer that reports these warnings
            if (publicConstructors.Length == 0)
            {
                return null;
            }

            foreach (var constructor in publicConstructors)
            {
                var parameterList = constructor.ChildNodes().OfType<ParameterListSyntax>().First();
                var isValid = true;

                var constructorParameters = new List<StartupSystemConstructorParameter>();

                foreach (var parameter in parameterList.Parameters)
                {
                    var parameterTypeInfo = semanticModel.GetTypeInfo(parameter.Type);

                    var isResource = DoesTypeInfoImplementInterface(parameterTypeInfo, "MyEngine.Core.Ecs.Resources.IResource");
                    var isQuery = TryGetQueryParameter(parameterTypeInfo, out var queryParameters);

                    if (isResource)
                    {
                        constructorParameters.Add(new StartupSystemConstructorParameter { Name = parameterTypeInfo.Type.ToDisplayString() });
                    }
                    else if (isQuery)
                    {
                        isValid = false;
                        break;
                    }
                    else
                    {
                        isValid = false;
                        break;
                    }
                }
                if (isValid)
                {
                    return new StartupSystemConstructor(constructorParameters);
                }
            }

            return null;
        }

        private SystemConstructor TryGetSystemConstructor(SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            var constructorDeclarations = classNode.ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .ToArray();

            // no constructor means public constructor
            if (constructorDeclarations.Length == 0)
            {
                return SystemConstructor.NoConstructor;
            }

            var publicConstructors = constructorDeclarations
                .Where(x => x.ChildTokens().Any(y => y.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToArray();

            // todo: add analyzer that reports these warnings
            if (publicConstructors.Length == 0)
            {
                return null;
            }

            foreach (var constructor in publicConstructors)
            {
                var parameterList = constructor.ChildNodes().OfType<ParameterListSyntax>().First();
                var isValid = true;

                var constructorParameters = new List<SystemConstructorParameter>();

                foreach (var parameter in parameterList.Parameters)
                {
                    var parameterTypeInfo = semanticModel.GetTypeInfo(parameter.Type);

                    var isResource = DoesTypeInfoImplementInterface(parameterTypeInfo, "MyEngine.Core.Ecs.Resources.IResource");
                    var isQuery = TryGetQueryParameter(parameterTypeInfo, out var queryParameters);

                    if (isResource)
                    {
                        constructorParameters.Add(new SystemConstructorParameter { IsResource = true, Name = parameterTypeInfo.Type.ToDisplayString() });
                    }
                    else if (isQuery)
                    {
                        var namedType = parameterTypeInfo.Type as INamedTypeSymbol;
                        constructorParameters.Add(new SystemConstructorParameter { Name = namedType.ToDisplayString(), QueryComponentTypeParameters = queryParameters });
                    }
                    else
                    {
                        isValid = false;
                        break;
                    }
                }
                if (isValid)
                {
                    return new SystemConstructor(constructorParameters);
                }
            }

            return null;
        }

        private bool TryGetQueryParameter(TypeInfo parameterTypeInfo, out IReadOnlyList<QueryComponentTypeParameter> queryParameters)
        {
            var queryParametersList = new List<QueryComponentTypeParameter>();
            queryParameters = queryParametersList;
            if (!parameterTypeInfo.Type.ToDisplayString().StartsWith("MyEngine.Core.Ecs.IQuery<"))
            {
                return false;
            }

            var namedType = parameterTypeInfo.Type as INamedTypeSymbol;


            foreach (var argument in namedType.TypeArguments)
            {
                var argumentDisplay = argument.ToDisplayString();
                const string optionalComponentStart = "MyEngine.Core.Ecs.Components.OptionalComponent<";
                if (argumentDisplay.StartsWith(optionalComponentStart))
                {
                    queryParametersList.Add(new QueryComponentTypeParameter
                    {
                        MetaComponentType = MetaComponentType.OptionalComponent,
                        ComponentTypeName = argumentDisplay.Substring(optionalComponentStart.Length, argumentDisplay.Length - optionalComponentStart.Length - 1)
                    });
                }
                else
                {
                    queryParametersList.Add(new QueryComponentTypeParameter()
                    {
                        ComponentTypeName = argumentDisplay
                    });
                }
            }

            return true;

        }

        private bool IsClassConcreteAndAccessible(SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            var childTokens = classNode.ChildTokens().ToArray();
            if (childTokens.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AbstractKeyword)))
            {
                // class is abstract, so can't be constructed

                // todo: error
                return false;
            }

            if (!childTokens.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
            {
                if (childTokens.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword))
                    && (semanticModel.Compilation.AssemblyName == "MyEngine.Core"
                    || semanticModel.Compilation.AssemblyName == "MyEngine.Input"))
                {
                    // this is one of our projects, and the system is internal, so we can access it from the runtime
                    return true;
                }
                // class is not public, so can't be accessed

                // todo: error
                return false;
            }

            return true;
        }

        private bool DoesClassNodeImplementInterface(SemanticModel semanticModel, ClassDeclarationSyntax classNode, string interfaceFullyQualifiedName)
        {
            if (classNode.BaseList is null)
            {
                return false;
            }

            foreach (var type in classNode.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(type.Type);
                if (DoesTypeInfoImplementInterface(typeInfo, interfaceFullyQualifiedName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool DoesTypeInfoImplementInterface(TypeInfo typeInfo, string interfaceFullyQualifiedName)
        {
            if (typeInfo.Type is null)
            {
                return false;
            }

            return typeInfo.Type.ToDisplayString() == interfaceFullyQualifiedName
                || typeInfo.Type.AllInterfaces.Any(x => x.ToDisplayString() == interfaceFullyQualifiedName);
        }

        private bool DoesEntrypointHaveAccessibleConstructor(ClassDeclarationSyntax classNode)
        {
            var constructorDeclarations = classNode.ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .ToArray();

            // no constructor means public constructor
            if (constructorDeclarations.Length == 0)
            {
                return true;
            }

            var publicConstructors = constructorDeclarations
                .Where(x => x.ChildTokens().Any(y => y.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToArray();

            // todo: add analyzer that reports these warnings
            if (publicConstructors.Length == 0)
            {
                return false;
            }

            var emptyConstructor = publicConstructors
                .FirstOrDefault(x =>
                {
                    var parameterList = x.ChildNodes().OfType<ParameterListSyntax>().First();
                    return parameterList.Parameters.Count == 0;
                });

            if (emptyConstructor is null)
            {
                return false;
            }

            // found a public empty constructor 
            return true;
        }
    }

}
