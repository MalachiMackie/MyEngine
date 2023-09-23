using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorHelperTests;
public class DoesClassHaveAccessibleConstructor
{
    private readonly SourceGeneratorHelpers _helpers = new();

    [Fact]
    public void Should_ReturnTrue_When_NoConstructorExists()
    {
        var source = """
            public class MyClass
            {
            }
            """;

        var classNode = GetClassNode(source);
        
        var result = _helpers.DoesClassHaveAccessibleEmptyConstructor(classNode);

        result.Should().BeTrue();
    }

    [Fact]
    public void Should_ReturnTrue_When_ConstructorIsPublic()
    {
        var source = """
            public class MyClass
            {
                public MyClass()
                {
                }
            }
            """;

        var classNode = GetClassNode(source);

        var result = _helpers.DoesClassHaveAccessibleEmptyConstructor(classNode);

        result.Should().BeTrue();
    }

    [Fact]
    public void Should_ReturnTrue_When_MultipleConstructorsExist()
    {
        var source = """
            public class MyClass
            {
                private MyClass(string someParam)
                {
                }

                public MyClass()
                {
                }
            }
            """;

        var classNode = GetClassNode(source);

        var result = _helpers.DoesClassHaveAccessibleEmptyConstructor(classNode);

        result.Should().BeTrue();
    }

    [Fact]
    public void Should_ReturnFalse_When_ConstructorIsPrivate()
    {
        var source = """
            public class MyClass
            {
                private MyClass()
                {
                }
            }
            """;

        var classNode = GetClassNode(source);

        var result = _helpers.DoesClassHaveAccessibleEmptyConstructor(classNode);

        result.Should().BeFalse();
    }

    [Fact]
    public void Should_ReturnFalse_When_ConstructorIsNotEmpty()
    {
        var source = """
            public class MyClass
            {
                public MyClass(string someParam)
                {
                }
            }
            """;

        var classNode = GetClassNode(source);
        var result = _helpers.DoesClassHaveAccessibleEmptyConstructor(classNode);

        result.Should().BeFalse();
    }

    private static ClassDeclarationSyntax GetClassNode(string source)
    {
        var compilation = SourceGeneratorTestHelpers.CreateCompilation(source,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();

        var classNode = syntaxTree.GetRoot().DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .Single();

        return classNode;

    }
}
