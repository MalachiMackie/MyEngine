using System.Collections;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorHelperTests;
public class IsClassConcreteAndAccessible
{

    private readonly SourceGeneratorHelpers _helper = new();

    [Theory]
    [ClassData(typeof(InternalClassesWithInternalsVisibleTo))]
    public void Should_ReturnTrue_When_ClassIsInternalButInternalsVisibleToIsFound(string source)
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

        var result = _helper.IsClassConcreteAndAccessible(semanticModel, classNode);
        result.Should().BeTrue();

    }

    [Theory]
    [ClassData(typeof(NonConcreteAndAccessibleClasses))]
    public void Should_ReturnFalse_When_ItIsNot(string source, string className)
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
            .First(x => semanticModel.GetDeclaredSymbol(x)?.Name == className);

        var result = _helper.IsClassConcreteAndAccessible(semanticModel, classNode);
        result.Should().BeFalse();
    }

    [Theory]
    [ClassData(typeof(ConcreteAndAccessibleClasses))]
    public void Should_ReturnTrue_When_ItIs(string source, string className)
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
            .First(x => semanticModel.GetDeclaredSymbol(x)?.Name == className);

        var result = _helper.IsClassConcreteAndAccessible(semanticModel, classNode);
        result.Should().BeTrue();
    }
}

public class InternalClassesWithInternalsVisibleTo : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyEngine.Runtime")]
            internal class MyClass
            {
            }
            """
        };

        yield return new object[]
        {
            """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyEngine.Runtime")]
            class MyClass
            {
            }
            """
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class ConcreteAndAccessibleClasses : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            """
            public class MyClass
            {
            }
            """,
            "MyClass"
        };

        yield return new object[]
        {
            """
            public class OtherClass
            {
                public class MyClass
                {
                }
            }
            """,
            "MyClass"
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class NonConcreteAndAccessibleClasses : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            """
            internal class MyClass
            {
                public MyClass() {}
            }
            """,
            "MyClass"
        };

        yield return new object[]
        {
            """
            class MyClass
            {
            }
            """,
            "MyClass"
        };

        yield return new object[]
        {
            """
            public class OtherClass
            {
                private class MyClass
                {
                }
            }
            """,
            "MyClass"
        };

        yield return new object[]
        {
            """
            public abstract class MyClass
            {
            }
            """,
            "MyClass"
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
