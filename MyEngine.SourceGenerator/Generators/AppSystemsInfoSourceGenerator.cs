using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace MyEngine.SourceGenerator.Generators
{
    [Generator]
    internal class AppSystemsInfoSourceGenerator : IIncrementalGenerator
    {
        private readonly ISourceGeneratorHelpers _helpers;

        public AppSystemsInfoSourceGenerator()
        {
            _helpers = new SourceGeneratorHelpers();
        }

        public AppSystemsInfoSourceGenerator(ISourceGeneratorHelpers helpers)
        {
            _helpers = helpers;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classNodes = context.SyntaxProvider.CreateSyntaxProvider((x, _) => x is ClassDeclarationSyntax, (x, _) => (x.SemanticModel, ClassNode: x.Node as ClassDeclarationSyntax));

            var accessibleClassNodes = classNodes
                .Where(x => _helpers.IsClassConcreteAndAccessible(x.SemanticModel, x.ClassNode));

            var compilationValue = context.CompilationProvider.Select((x, _) => x);

            var startupSystemClasses = accessibleClassNodes
                .Where(x => _helpers.DoesClassNodeImplementInterface(x.SemanticModel, x.ClassNode, "MyEngine.Core.Ecs.Systems.IStartupSystem"))
                .Select((x, _) => (x.SemanticModel, x.ClassNode, Constructor: TryGetStartupSystemConstructor(x.SemanticModel, x.ClassNode)))
                .Where(x => x.Constructor != null)
                .Select((x, _) => new StartupSystemClass(_helpers.GetFullyQualifiedName(x.ClassNode), x.Constructor))
                .Collect();

            var systemClasses = accessibleClassNodes
                .Where(x => _helpers.DoesClassNodeImplementInterface(x.SemanticModel, x.ClassNode, "MyEngine.Core.Ecs.Systems.ISystem"))
                .Select((x, _) => (x.SemanticModel, x.ClassNode, Constructor: TryGetSystemConstructor(x.SemanticModel, x.ClassNode)))
                .Where(x => x.Constructor != null)
                .Select((x, _) => new SystemClass(_helpers.GetFullyQualifiedName(x.ClassNode), x.Constructor))
                .Collect();

            var allSystemsAndCompilation = startupSystemClasses.Combine(systemClasses)
                .Combine(compilationValue);

            context.RegisterImplementationSourceOutput(allSystemsAndCompilation, (sourceProductionContext, classesAndCompilation) =>
            {
                var ((startupSystems, systems), compilation) = classesAndCompilation;

                // todo: assembly attribute instead
                if (compilation.AssemblyName == "MyEngine.Runtime")
                {
                    return;
                }

                var template = SourceTemplate.LoadFromEmbeddedResource("AppSystemsInfo.template");
                template.SubstitutePart("Namespace", $"{compilation.AssemblyName}.Generated");
                template.SubstitutePart("SystemClasses", JsonConvert.SerializeObject(systems.Select(x => new SystemClassDto(x))).Replace("\"", "\\\""));
                template.SubstitutePart("StartupSystemClasses", JsonConvert.SerializeObject(startupSystems.Select(x => new StartupSystemClassDto(x))).Replace("\"", "\\\""));

                sourceProductionContext.AddSource("AppSystemsInfo.g.cs", template.Build());
            });
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

                    var isResource = _helpers.DoesTypeInfoImplementInterface(parameterTypeInfo, "MyEngine.Core.Ecs.Resources.IResource");
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
                return new SystemConstructor();
            }

            var publicConstructors = constructorDeclarations
                .Where(x => x.ChildTokens().Any(y => y.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToArray();

            // todo: add analyzer that reports these warnings
            if (publicConstructors.Length == 0)
            {
                return null;
            }

            foreach (var constructorNode in publicConstructors)
            {
                var parameterList = constructorNode.ChildNodes().OfType<ParameterListSyntax>().First();
                var isValid = true;
                var constructor = new SystemConstructor();

                foreach (var parameter in parameterList.Parameters)
                {
                    var parameterTypeInfo = semanticModel.GetTypeInfo(parameter.Type);

                    if (_helpers.DoesTypeInfoImplementInterface(parameterTypeInfo, "MyEngine.Core.Ecs.Resources.IResource"))
                    {
                        constructor.AddParameter(new SystemConstructorResourceParameter(parameterTypeInfo.Type.ToDisplayString()));
                    }
                    else if (TryGetQueryParameter(parameterTypeInfo, out var queryParameter))
                    {
                        constructor.AddParameter(queryParameter);
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

            return null;
        }

        private bool TryGetQueryParameter(TypeInfo parameterTypeInfo, out SystemConstructorQueryParameter queryParameter)
        {
            var queryParametersList = new List<QueryComponentTypeParameter>();
            if (!parameterTypeInfo.Type.ToDisplayString().StartsWith("MyEngine.Core.Ecs.IQuery<"))
            {
                queryParameter = null;
                return false;
            }

            var namedType = parameterTypeInfo.Type as INamedTypeSymbol;
            if (namedType.TypeArguments.Length == 0)
            {
                queryParameter = null;
                return false;
            }

            QueryComponentTypeParameter GetTypeParameter(ITypeSymbol argument)
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

            var firstTypeParameter = GetTypeParameter(namedType.TypeArguments[0]);
            var restTypeParameter = namedType.TypeArguments.Skip(1).Select(GetTypeParameter);

            queryParameter = new SystemConstructorQueryParameter(firstTypeParameter, restTypeParameter);
            return true;

        }
    }
}
