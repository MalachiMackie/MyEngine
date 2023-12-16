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

        var constructors = allNodes
            .OfType<ConstructorDeclarationSyntax>();

        var classNode = allNodes.OfType<ClassDeclarationSyntax>().Single();

        var result = _helper.FilterConstructorsToAccessible(semanticModel, classNode, constructors);

        result.Should().ContainSingle()
            .Which.ParameterList.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Should_OnlyReturnPublicAndInternalConstructors_When_InternalsVisibleIsTrue()
    {
        var source = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyEngine.Runtime")]

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

        var constructors = allNodes
            .OfType<ConstructorDeclarationSyntax>();

        var classNode = allNodes.OfType<ClassDeclarationSyntax>().Single();

        var result = _helper.FilterConstructorsToAccessible(semanticModel, classNode, constructors);

        result.Should().HaveCount(2)
            .And.ContainSingle(x => x.ParameterList.Parameters.Select(x => x.Type).OfType<PredefinedTypeSyntax>().Any(y => y.Keyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringKeyword)))
            .And.ContainSingle(x => !x.ParameterList.Parameters.Any());
    }
}
