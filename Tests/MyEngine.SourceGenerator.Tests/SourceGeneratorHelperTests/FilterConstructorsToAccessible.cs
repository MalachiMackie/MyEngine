using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorHelperTests;

public class FilterConstructorsToAccessible
{
    private readonly SourceGeneratorHelpers _helper = new();

    [Fact]
    public void Should_FilterAllNonPublicConstructors_When_InternalsVisibleIsFalse()
    {
        var source = """
            public class MyClass
            {
                public MyClass(){}

                internal MyClass(string param){}

                MyClass(float param){}

                protected MyClass(int param){}

                private MyClass(double param){}
            }
            """;

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var allNodes = syntaxTree.GetRoot().DescendantNodesAndSelf();

        var classNode = allNodes.OfType<ClassDeclarationSyntax>().Single();

        var result = _helper.GetAccessibleConstructors((semanticModel.GetDeclaredSymbol(classNode) as INamedTypeSymbol)!);

        result.Should().ContainSingle()
            .Which.Parameters.Should().BeEmpty();
    }
}
