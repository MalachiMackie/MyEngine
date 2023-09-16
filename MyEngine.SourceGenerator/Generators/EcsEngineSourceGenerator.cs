using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace MyEngine.SourceGenerator.Generators
{
    [Generator]
    public class EcsEngineSourceGenerator : IIncrementalGenerator
    {
        private readonly ISourceGeneratorHelpers _helpers;
        
        public EcsEngineSourceGenerator()
        {
            _helpers = new SourceGeneratorHelpers();
        }

        public EcsEngineSourceGenerator(ISourceGeneratorHelpers helpers)
        {
            _helpers = helpers;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classNodes = context.SyntaxProvider.CreateSyntaxProvider((x, _) => x is ClassDeclarationSyntax, (x, _) => (x.SemanticModel, ClassNode: x.Node as ClassDeclarationSyntax));
            var accessibleClassNodes = classNodes
                .Where(x => _helpers.IsClassConcreteAndAccessible(x.SemanticModel, x.ClassNode));

            var compilationValue = context.CompilationProvider.Select((x, _) => x);


            var appSystemsInfoTypes = compilationValue.SelectMany((x, _) => x.References.Select(y => x.GetAssemblyOrModuleSymbol(y)).OfType<IAssemblySymbol>())
                .SelectMany((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace))
                .Where(x => x.GetAttributes().Any(y => y.AttributeClass?.ToDisplayString() == "MyEngine.Core.AppSystemsInfoAttribute"))
                .Collect();

            var assemblyName = compilationValue.Select((x, _) => x.AssemblyName);

            // todo: use attribute instead
            var appEntrypointInfoType = compilationValue.Select((x, _) => x.GetTypeByMetadataName("MyEngine.Runtime.AppEntrypointInfo"));

            var allTypes = appSystemsInfoTypes.Combine(assemblyName)
                .Combine(appEntrypointInfoType);

            context.RegisterImplementationSourceOutput(allTypes, (sourceProductionContext, value) =>
            {
                var ((appSystemsInfos, assemblyNameValue), appEntrypointInfo) = value;

                // todo: attribute instead
                if (assemblyNameValue != "MyEngine.Runtime")
                {
                    return;
                }

                var appEntrypointFullyQualifiedName = appEntrypointInfo.GetMembers().FirstOrDefault(x => x.Name == "FullyQualifiedName") as IFieldSymbol;

                var systemClassModels = appSystemsInfos.Select(x => x.GetMembers().FirstOrDefault(y => y.Name == "SystemClasses") as IFieldSymbol)
                    .SelectMany(x => JsonConvert.DeserializeObject<SystemClassDto[]>((x.ConstantValue as string).Replace("\\\"", "\"")))
                    .ToArray();

                var startupSystemClassModels = appSystemsInfos.Select(x => x.GetMembers().FirstOrDefault(y => y.Name == "StartupSystemClasses") as IFieldSymbol)
                    .SelectMany(x => JsonConvert.DeserializeObject<StartupSystemClassDto[]>((x.ConstantValue as string).Replace("\\\"", "\"")))
                    .ToArray();

                var (ecsEngineFileName, ecsEngineSource) = EcsEngineSourceBuilder.BuildEcsEngineSource(
                    startupSystemClassModels, systemClassModels, appEntrypointFullyQualifiedName.ConstantValue.ToString());

                sourceProductionContext.AddSource(ecsEngineFileName, ecsEngineSource);
            });

        }
    }

}
