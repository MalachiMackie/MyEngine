using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests;
public class SourceGeneratorHelperTests
{
    private readonly SourceGeneratorHelpers _helper = new();

    [Fact]
    public void GetAllNamespaceTypes_Should_GetAllNestedTypes()
    {
        var globalNamespace = A.Fake<INamespaceSymbol>();
        var level1ANamespace = A.Fake<INamespaceSymbol>();
        var level1BNamespace = A.Fake<INamespaceSymbol>();
        var level2Namespace = A.Fake<INamespaceSymbol>();

        var globalTypes = Array.Empty<INamedTypeSymbol>();
        var level1ATypes = new[] { A.Fake<INamedTypeSymbol>(x => x.Named("Type1")), A.Fake<INamedTypeSymbol>(x => x.Named("Type2")) };
        var level1BTypes = new[] { A.Fake<INamedTypeSymbol>(x => x.Named("Type3")) };
        var level2Types = new[] { A.Fake<INamedTypeSymbol>(x => x.Named("Type4")) };
        A.CallTo(() => globalNamespace.GetTypeMembers())
            .Returns(ImmutableArray.Create(globalTypes));
        A.CallTo(() => globalNamespace.GetNamespaceMembers())
            .Returns(ImmutableArray.Create(level1ANamespace, level1BNamespace));
        A.CallTo(() => level1ANamespace.GetTypeMembers())
            .Returns(ImmutableArray.Create(level1ATypes));
        A.CallTo(() => level1BNamespace.GetTypeMembers())
            .Returns(ImmutableArray.Create(level1BTypes));
        A.CallTo(() => level1ANamespace.GetNamespaceMembers())
            .Returns(ImmutableArray.Create<INamespaceSymbol>());
        A.CallTo(() => level1BNamespace.GetNamespaceMembers())
            .Returns(ImmutableArray.Create(level2Namespace));
        A.CallTo(() => level2Namespace.GetTypeMembers())
            .Returns(ImmutableArray.Create(level2Types));
        A.CallTo(() => level2Namespace.GetNamespaceMembers())
            .Returns(ImmutableArray.Create<INamespaceSymbol>());

        var result = _helper.GetAllNamespaceTypes(globalNamespace);

        var expectedTypes = globalTypes.Concat(level1ATypes)
            .Concat(level1BTypes)
            .Concat(level2Types);

        result.Should().BeEquivalentTo(expectedTypes, opts => opts.WithoutStrictOrdering());
    }

    [Fact]
    public void GetFullyQualifiedName_Should_GetFullyQualifiedName()
    {

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            """
            namespace MyNamespace.Classes
            {
                public class MyClass
                {

                }
            }
            
            """,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var classNode = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var result = _helper.GetFullyQualifiedName(semanticModel, classNode);
        result.Should().Be("global::MyNamespace.Classes.MyClass");
    }

    [Theory]
    [ClassData(typeof(ConcreteAndAccessibleClasses))]
    public void IsClassConcreteAndAccessible_Should_ReturnTrue_When_ItIs(string source, string className)
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

    [Theory]
    [ClassData(typeof(NonConcreteAndAccessibleClasses))]
    public void IsClassConcreteAndAccessible_Should_ReturnFalse_When_ItIsNot(string source, string className)
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

    [Fact]
    public void DoesAssemblyGiveEngineRuntimeAccessToInternals_Should_ReturnTrue_When_InternalsVisibleToIsFound()
    {
        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyEngine.Runtime")]
            """,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var result = _helper.DoesAssemblyGiveEngineRuntimeAccessToInternals(compilation.Assembly);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"OtherEngine.Runtime\")]")]
    [InlineData("")]
    public void DoesAssemblyGiveEngineRuntimeAccessToInternals_Should_ReturnFalse_When_NoMatchingAttributeIsFound(string source)
    {
        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var result = _helper.DoesAssemblyGiveEngineRuntimeAccessToInternals(compilation.Assembly);
        result.Should().BeFalse();
    }


    [Theory]
    [ClassData(typeof(InternalClassesWithInternalsVisibleTo))]
    public void IsClassConcreteAndAccessible_Should_ReturnTrue_When_ClassIsInternalButInternalsVisibleToIsFound(string source)
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

