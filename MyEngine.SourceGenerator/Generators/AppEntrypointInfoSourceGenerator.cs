﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyEngine.SourceGenerator.Generators
{
    [Generator]
    internal class AppEntrypointInfoSourceGenerator : IIncrementalGenerator
    {
        private readonly SourceGeneratorHelpers _helpers;

        public AppEntrypointInfoSourceGenerator()
        {
            _helpers = new SourceGeneratorHelpers();
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var nodesWithAppEntrypointAttribute = context.SyntaxProvider.ForAttributeWithMetadataName(
                SourceGeneratorHelpers.AttributeNames[EngineAttribute.AppEntrypoint].FullyQualifiedName,
                (syntaxNode, _) => true,
                (generatorAttributeSyntaxContext, _) => (generatorAttributeSyntaxContext.SemanticModel, ClassNode: generatorAttributeSyntaxContext.TargetNode))
                .Where(x => x.ClassNode is ClassDeclarationSyntax)
                .Select((x, _) => (x.SemanticModel, ClassNode: (ClassDeclarationSyntax)x.ClassNode))
                .Where(x => _helpers.IsClassConcreteAndAccessible(x.SemanticModel, x.ClassNode))
                .Where(x => _helpers.DoesClassNodeImplementInterface(x.SemanticModel, x.ClassNode, "MyEngine.Core.IAppEntrypoint"))
                .Where(x => _helpers.DoesClassHaveAccessibleConstructor(x.ClassNode));

            context.RegisterImplementationSourceOutput(nodesWithAppEntrypointAttribute, (sourceProductionContext, semanticModelAndClassNode) =>
            {
                var classNode = semanticModelAndClassNode.ClassNode;

                var template = SourceTemplate.LoadFromEmbeddedResource("AppEntrypointInfo.template");
                template.SubstitutePart("AppEntrypointInfoAttribute", SourceGeneratorHelpers.AttributeNames[EngineAttribute.AppEntrypointInfo].CodeUsage);
                template.SubstitutePart("Namespace", $"{semanticModelAndClassNode.SemanticModel.Compilation.AssemblyName}.Generated");
                template.SubstitutePart("FullyQualifiedNameAttribute", SourceGeneratorHelpers.AttributeNames[EngineAttribute.AppEntrypointInfoFullyQualifiedName].CodeUsage);
                template.SubstitutePart("FullyQualifiedName", _helpers.GetFullyQualifiedName(semanticModelAndClassNode.SemanticModel, classNode));

                sourceProductionContext.AddSource("AppEntrypointInfo.g.cs", template.Build());
            });
        }
    }
}
