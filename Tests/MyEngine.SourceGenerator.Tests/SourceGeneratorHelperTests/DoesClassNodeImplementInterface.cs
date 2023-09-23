using System.Collections;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorHelperTests;

public class DoesClassNodeImplementInterface
{

    private readonly SourceGeneratorHelpers _helper = new();

    [Theory]
    [ClassData(typeof(ClassImplementingInterface))]
    public void Should_ReturnTrue_When_ItDoes(string source, string interfaceFullyQualifiedName)
    {
        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var classNode = syntaxTree.GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var result = _helper.DoesClassNodeImplementInterface(semanticModel, classNode, interfaceFullyQualifiedName);
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_ReturnFalse_When_ThereIsNoBaseList()
    {
        var source = """
            public interface MyInterface
            {
            }

            public class MyClass
            {
            }
            """;

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
                    source,
                    Array.Empty<KeyValuePair<string, string>>(),
                    Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var classNode = syntaxTree.GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var result = _helper.DoesClassNodeImplementInterface(semanticModel, classNode, "MyInterface");
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_ReturnFalse_When_NoBasesImplementInterface()
    {
        var source = """
            public interface MyInterface
            {
            }
            public interface OtherInterface
            {
            }
            public class MyClass : OtherInterface
            {
            }
            """;

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
                    source,
                    Array.Empty<KeyValuePair<string, string>>(),
                    Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var classNode = syntaxTree.GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var result = _helper.DoesClassNodeImplementInterface(semanticModel, classNode, "MyInterface");
        result.Should().BeFalse();
    }
}

public class ClassImplementingInterface : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            """
            public interface MyInterface
            {
            }

            public class MyClass : MyInterface
            {
            }
            """,
            "MyInterface"
        };

        yield return new object[]
        {
            """
            public interface OtherInterface
            {
            }

            public interface MyInterface
            {
            }

            public class MyClass : OtherInterface, MyInterface
            {
            }
            """,
            "MyInterface"
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
