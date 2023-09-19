using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

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
            var classNodes = context.SyntaxProvider.CreateSyntaxProvider((x, _) => x is ClassDeclarationSyntax, (x, _) => (x.SemanticModel, ClassNode: x.Node as ClassDeclarationSyntax));
            var accessibleClassNodes = classNodes
                .Where(x => _helpers.IsClassConcreteAndAccessible(x.SemanticModel, x.ClassNode!));

            var compilationValue = context.CompilationProvider.Select((x, _) => x);

            var appSystemsInfoTypes = compilationValue.SelectMany((x, _) => x.References.Select(y => x.GetAssemblyOrModuleSymbol(y)).OfType<IAssemblySymbol>())
                .SelectMany((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace))
                .Where(x => x.GetAttributes().Any(y => y.AttributeClass?.ToDisplayString() == "MyEngine.Core.AppSystemsInfoAttribute"))
                .Collect();

            var assemblyAttributesProvider = compilationValue.Select((x, _) => x.Assembly.GetAttributes());

            // todo: use attribute instead
            var appEntrypointInfoType = compilationValue.Select((x, _) => x.GetTypeByMetadataName("MyEngine.Runtime.AppEntrypointInfo"));

            var allTypes = appSystemsInfoTypes.Combine(assemblyAttributesProvider)
                .Combine(appEntrypointInfoType);

            context.RegisterImplementationSourceOutput(allTypes, (sourceProductionContext, value) =>
            {
                var ((appSystemsInfos, assemblyAttributes), appEntrypointInfo) = value;

                if (assemblyAttributes.Length == 0 || assemblyAttributes.All(x => x.AttributeClass is null || x.AttributeClass.ToDisplayString() != "MyEngine.Runtime.EngineRuntimeAssemblyAttribute"))
                {
                    return;
                }

                if (appEntrypointInfo is null)
                {
                    return;
                }

                if (!(appEntrypointInfo.GetMembers().FirstOrDefault(x =>
                    x.Name == "FullyQualifiedName"
                    && x is IFieldSymbol fieldSymbol
                    && fieldSymbol.HasConstantValue
                    && fieldSymbol.ConstantValue is string) is IFieldSymbol appEntrypointFullyQualifiedNameField))
                {
                    return;
                }

                var appEntrypointFullyQualifiedName = (string)appEntrypointFullyQualifiedNameField.ConstantValue!;

                var systemClassModels = appSystemsInfos.Select(x => x.GetMembers().FirstOrDefault(y => y.Name == "SystemClasses"))
                    .Where(x => x is IFieldSymbol)
                    .Cast<IFieldSymbol>()
                    .Where(x => x.HasConstantValue)
                    .Select(x => x.ConstantValue)
                    .Where(x => x is string)
                    .Cast<string>()
                    .SelectMany(x => JsonConvert.DeserializeObject<SystemClassDto[]>(x.Replace("\\\"", "\"")))
                    .ToArray();

                var startupSystemClassModels = appSystemsInfos.Select(x => x.GetMembers().FirstOrDefault(y => y.Name == "StartupSystemClasses"))
                    .Where(x => x is IFieldSymbol)
                    .Cast<IFieldSymbol>()
                    .Where(x => x.HasConstantValue)
                    .Select(x => x.ConstantValue)
                    .Cast<string>()
                    .SelectMany(x => JsonConvert.DeserializeObject<StartupSystemClassDto[]>(x.Replace("\\\"", "\"")))
                    .ToArray();

                var (ecsEngineFileName, ecsEngineSource) = EcsEngineSourceBuilder.BuildEcsEngineSource(
                    startupSystemClassModels, systemClassModels, appEntrypointFullyQualifiedName);

                sourceProductionContext.AddSource(ecsEngineFileName, ecsEngineSource);
            });

        }
    }

}
