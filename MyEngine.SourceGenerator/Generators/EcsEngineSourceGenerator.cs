using System.Linq;
using Microsoft.CodeAnalysis;
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

            var allReferenceTypes = compilationValue.SelectMany((x, _) => x.References.Select(y => x.GetAssemblyOrModuleSymbol(y)).OfType<IAssemblySymbol>())
                .SelectMany((x, _) => _helpers.GetAllNamespaceTypes(x.GlobalNamespace));

            var appSystemsInfoValues = allReferenceTypes
                .Where(x => x.GetAttributes().Any(y => _helpers.DoesAttributeMatch(y, EngineAttribute.AppSystemsInfo)))
                .Select((x, _) => GetAppSystemsInfoValues(x))
                .Collect();

            var assemblyAttributesProvider = compilationValue.Select((x, _) => x.Assembly.GetAttributes());

            var appEntrypointFullyQualifiedName = allReferenceTypes
                .Where(x => x.GetAttributes().Any(y => _helpers.DoesAttributeMatch(y, EngineAttribute.AppEntrypointInfo)))
                .Select((x, _) => GetAppEntrypointFullyQualifiedName(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .Collect()
                .Select((x, _) => x.FirstOrDefault());

            var allTypes = appSystemsInfoValues.Combine(assemblyAttributesProvider)
                .Combine(appEntrypointFullyQualifiedName);

            context.RegisterImplementationSourceOutput(allTypes, (sourceProductionContext, value) =>
            {
                var ((appSystemsInfoValues, assemblyAttributes), appEntrypointFullyQualifiedName) = value;

                if (assemblyAttributes.Length == 0 || assemblyAttributes.All(x => !_helpers.DoesAttributeMatch(x, EngineAttribute.EngineRuntimeAssembly)))
                {
                    return;
                }

                if (appEntrypointFullyQualifiedName is null)
                {
                    return;
                }

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

        private string? GetAppEntrypointFullyQualifiedName(ITypeSymbol appEntrypointInfoTypeSymbol)
        {
            return appEntrypointInfoTypeSymbol.GetMembers()
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .OfType<IFieldSymbol>()
                .Where(x => x.HasConstantValue)
                .Where(x => x.ConstantValue is string)
                .Where(x => x.GetAttributes().Any(y => _helpers.DoesAttributeMatch(y, EngineAttribute.AppEntrypointInfoFullyQualifiedName)))
                .FirstOrDefault()
                ?.ConstantValue as string;
        }

        private (SystemClassDto[]? SystemClasses, StartupSystemClassDto[]? StartupSystemClasses) GetAppSystemsInfoValues(ITypeSymbol appSystemsInfoType)
        {
            var publicStringFieldMembers = appSystemsInfoType.GetMembers()
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .OfType<IFieldSymbol>()
                .Where(x => x.HasConstantValue)
                .Where(x => x.ConstantValue is string);

            var systemClassesMember = publicStringFieldMembers.FirstOrDefault(x => x.GetAttributes()
                    .Any(y => _helpers.DoesAttributeMatch(y, EngineAttribute.SystemClasses)));

            var startupSystemClassesMember = publicStringFieldMembers.FirstOrDefault(x => x.GetAttributes()
                    .Any(y => _helpers.DoesAttributeMatch(y, EngineAttribute.StartupSystemClasses)));

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
