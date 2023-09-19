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
            var compilationValue = context.CompilationProvider.Select((x, _) => x);

            var appSystemsInfoValues = compilationValue.SelectMany((x, _) => x.References.Select(y => x.GetAssemblyOrModuleSymbol(y)).OfType<IAssemblySymbol>())
                .SelectMany((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace))
                .Where(x => x.GetAttributes().Any(y => y.AttributeClass?.ToDisplayString() == "MyEngine.Core.AppSystemsInfoAttribute"))
                .Select((x, _) => GetAppSystemsInfoValues(x))
                .Collect();

            var assemblyAttributesProvider = compilationValue.Select((x, _) => x.Assembly.GetAttributes());

            // todo: use attribute instead
            var appEntrypointInfoType = compilationValue.Select((x, _) => x.GetTypeByMetadataName("MyEngine.Runtime.AppEntrypointInfo"));

            var allTypes = appSystemsInfoValues.Combine(assemblyAttributesProvider)
                .Combine(appEntrypointInfoType);

            context.RegisterImplementationSourceOutput(allTypes, (sourceProductionContext, value) =>
            {
                var ((appSystemsInfoValues, assemblyAttributes), appEntrypointInfo) = value;

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
                    && fieldSymbol.DeclaredAccessibility == Accessibility.Public
                    && fieldSymbol.ConstantValue is string) is IFieldSymbol appEntrypointFullyQualifiedNameField))
                {
                    return;
                }

                var appEntrypointFullyQualifiedName = (string)appEntrypointFullyQualifiedNameField.ConstantValue!;

                var systemClassModels = appSystemsInfoValues
                    .Where(x => x.SystemClasses != null)
                    .SelectMany(x => x.SystemClasses)
                    .ToArray();

                var startupSystemClassModels = appSystemsInfoValues
                    .Where(x => x.StartupSystemClasses != null)
                    .SelectMany(x => x.StartupSystemClasses)
                    .ToArray();

                var (ecsEngineFileName, ecsEngineSource) = EcsEngineSourceBuilder.BuildEcsEngineSource(
                    startupSystemClassModels, systemClassModels, appEntrypointFullyQualifiedName);

                sourceProductionContext.AddSource(ecsEngineFileName, ecsEngineSource);
            });

        }

        private (SystemClassDto[]? SystemClasses, StartupSystemClassDto[]? StartupSystemClasses) GetAppSystemsInfoValues(ITypeSymbol appSystemsInfoType)
        {
            var publicStringFieldMembers = appSystemsInfoType.GetMembers()
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .OfType<IFieldSymbol>()
                .Where(x => x.HasConstantValue)
                .Where(x => x.ConstantValue is string);

            var systemClassesMember = publicStringFieldMembers.FirstOrDefault(x => x.GetAttributes()
                    .Any(y => y.AttributeClass != null
                            && y.AttributeClass.ToDisplayString() == "MyEngine.Core.SystemClassesAttribute"));

            var startupSystemClassesMember = publicStringFieldMembers.FirstOrDefault(x => x.GetAttributes()
                    .Any(y => y.AttributeClass != null
                            && y.AttributeClass.ToDisplayString() == "MyEngine.Core.StartupSystemClassesAttribute"));

            var systemClasses = systemClassesMember is null
                ? null
                : JsonConvert.DeserializeObject<SystemClassDto[]>((string)systemClassesMember.ConstantValue!);
            var startupSystemClasses = startupSystemClassesMember is null
                ? null
                : JsonConvert.DeserializeObject<StartupSystemClassDto[]>((string)startupSystemClassesMember.ConstantValue!);

            return (systemClasses, startupSystemClasses);
        }
    }

}
