using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyEngine.SourceGenerator.Generators
{
    [Generator]
    public class EcsEngineSourceGenerator : IIncrementalGenerator
    {
        private readonly SourceGeneratorHelpers _helpers;

        public EcsEngineSourceGenerator()
        {
            _helpers = new SourceGeneratorHelpers();
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilationValue = context.CompilationProvider.Select((x, _) => x);

            var assemblyAttributesProvider = compilationValue.Select((x, _) => x.Assembly.GetAttributes());

            var allReferenceTypes = compilationValue.SelectMany((x, _) => x.References.Select(y => x.GetAssemblyOrModuleSymbol(y)).OfType<IAssemblySymbol>())
                           .Select((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace).OfType<INamedTypeSymbol>());

            var assemblyTypes = compilationValue.SelectMany((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace).OfType<INamedTypeSymbol>());

            var allTypes = compilationValue.SelectMany((x, _) =>
                    x.References.Select(y => x.GetAssemblyOrModuleSymbol(y))
                    .OfType<IAssemblySymbol>()
                    .Concat(new[] { x.Assembly }))
                .SelectMany((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace).OfType<INamedTypeSymbol>());

            var entrypoint = assemblyTypes
                .Where(x => x.GetAttributes()
                    .Any(y => y.AttributeClass!.ToDisplayString() == SourceGeneratorHelpers.AttributeNames[EngineAttribute.AppEntrypoint].FullyQualifiedName))
                .Where(_helpers.IsClassConcreteAndAccessible)
                .Where(x => _helpers.DoesClassNodeImplementInterface(x, "MyEngine.Core.IAppEntrypoint"))
                .Where(x => _helpers.DoesClassHaveAccessibleEmptyConstructor(x))
                .Select((x, _) => x.ToDisplayString());

            var classNodes = context.SyntaxProvider.CreateSyntaxProvider((x, _) => x is ClassDeclarationSyntax, (x, _) => (x.SemanticModel, ClassNode: (ClassDeclarationSyntax)x.Node));

            var accessibleReferenceTypes = allTypes.Where(_helpers.IsClassConcreteAndAccessible);

            var startupSystemClasses = accessibleReferenceTypes
                            .Where(x => _helpers.DoesClassNodeImplementInterface(x, "MyEngine.Core.Ecs.Systems.IStartupSystem"))
                            .Select((x, _) => (TypeSymbol: x, Constructor: TryGetStartupSystemConstructor(x)))
                            .Where(x => x.Constructor != null)
                            .Select((x, _) => new StartupSystemClass(
                                x.TypeSymbol.ToDisplayString(),
                                x.Constructor!))
                            .Collect();

            var systemClasses = accessibleReferenceTypes
                .Where(x => _helpers.DoesClassNodeImplementInterface(x, "MyEngine.Core.Ecs.Systems.ISystem"))
                .Select((x, _) => (TypeSymbol: x, Constructor: TryGetSystemConstructor(x)))
                .Where(x => x.Constructor != null)
                .Select((x, _) => new SystemClass(
                    x.TypeSymbol.ToDisplayString(),
                    x.Constructor!))
                .Collect();

            var things = systemClasses.Combine(startupSystemClasses)
                .Combine(assemblyAttributesProvider)
                .Combine(entrypoint.Collect());

            context.RegisterImplementationSourceOutput(things, (sourceProductionContext, value) =>
            {
                var (((systemClasses, startupSystemClasses), assemblyAttributes), entrypoints) = value;
                if (assemblyAttributes.Length == 0 || assemblyAttributes.All(x => !_helpers.DoesAttributeMatch(x, EngineAttribute.AppEntrypoint)))
                {
                    return;
                }

                if (entrypoints.Length != 1)
                {
                    return;
                }

                var (ecsEngineFileName, ecsEngineSource) = EcsEngineGlueSourceBuilder.BuildEcsEngineGlueSource(
                                    startupSystemClasses, systemClasses, entrypoints[0]);

                sourceProductionContext.AddSource(ecsEngineFileName, ecsEngineSource);
            });
        }

        private StartupSystemConstructor? TryGetStartupSystemConstructor(INamedTypeSymbol classSymbol)
        {
            // no constructor means public constructor
            if (classSymbol.Constructors.Length == 0)
            {
                return StartupSystemConstructor.Empty;
            }

            var accessibleConstructors = _helpers.GetAccessibleConstructors(classSymbol);

            foreach (var constructor in accessibleConstructors)
            {
                var isValid = true;

                var constructorParameters = new List<StartupSystemConstructorParameter>();

                foreach (var parameter in constructor.Parameters)
                {
                    if (!(parameter.Type is INamedTypeSymbol parameterTypeInfo))
                    {
                        continue;
                    }

                    var isResource = _helpers.DoesTypeSymbolImplementInterface(parameterTypeInfo, "MyEngine.Core.Ecs.Resources.IResource");
                    var isQuery = TryGetQueryParameter(parameterTypeInfo, out var _);

                    if (isResource)
                    {
                        constructorParameters.Add(new StartupSystemConstructorParameter(parameterTypeInfo.ToDisplayString()));
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

            // todo: add analyzer that reports these warnings
            return null;
        }

        private SystemConstructor? TryGetSystemConstructor(INamedTypeSymbol classSymbol)
        {
            // no constructor means public constructor
            if (classSymbol.InstanceConstructors.Length == 0)
            {
                return new SystemConstructor();
            }

            var accessibleConstructors = _helpers.GetAccessibleConstructors(classSymbol);

            foreach (var constructorNode in accessibleConstructors)
            {
                var isValid = true;
                var constructor = new SystemConstructor();

                foreach (var parameter in constructorNode.Parameters)
                {
                    if (!(parameter.Type is INamedTypeSymbol parameterTypeInfo))
                    {
                        continue;
                    }

                    if (_helpers.DoesTypeSymbolImplementInterface(parameterTypeInfo, "MyEngine.Core.Ecs.Resources.IResource"))
                    {
                        constructor.AddParameter(new SystemConstructorResourceParameter(parameterTypeInfo.ToDisplayString()));
                    }
                    else if (TryGetQueryParameter(parameterTypeInfo, out var queryParameter))
                    {
                        constructor.AddParameter(queryParameter!);
                    }
                    else
                    {
                        isValid = false;
                        break;
                    }
                }
                if (isValid)
                {
                    return constructor;
                }
            }

            // todo: add analyzer that reports these warnings
            return null;
        }

        private bool TryGetQueryParameter(INamedTypeSymbol parameterTypeInfo, out SystemConstructorQueryParameter? queryParameter)
        {
            var queryParametersList = new List<QueryComponentTypeParameter>();
            if (!parameterTypeInfo.ToDisplayString().StartsWith("MyEngine.Core.Ecs.IQuery<"))
            {
                queryParameter = null;
                return false;
            }

            if (parameterTypeInfo.TypeArguments.Length == 0)
            {
                queryParameter = null;
                return false;
            }

            static QueryComponentTypeParameter GetTypeParameter(ITypeSymbol argument)
            {
                var argumentDisplay = argument.ToDisplayString();
                const string optionalComponentStart = "MyEngine.Core.Ecs.Components.OptionalComponent<";
                if (argumentDisplay.StartsWith(optionalComponentStart))
                {
                    return new QueryComponentTypeParameter(
                        argumentDisplay.Substring(optionalComponentStart.Length, argumentDisplay.Length - optionalComponentStart.Length - 1),
                        MetaComponentType.OptionalComponent);
                }

                return new QueryComponentTypeParameter(argumentDisplay, metaComponentType: null);
            }

            var firstTypeParameter = GetTypeParameter(parameterTypeInfo.TypeArguments[0]);
            var restTypeParameter = parameterTypeInfo.TypeArguments.Skip(1).Select(GetTypeParameter);

            queryParameter = new SystemConstructorQueryParameter(firstTypeParameter, restTypeParameter);
            return true;

        }
    }

}
