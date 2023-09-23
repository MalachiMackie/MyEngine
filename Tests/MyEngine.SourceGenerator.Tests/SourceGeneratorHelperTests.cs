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

    [Theory]
    [ClassData(typeof(TypeSymbolImplementingInterface))]
    public void DoesTypeSymbolImplementInterface_Should_ReturnTrue_When_ItDoes(string source, string symbolName, string interfaceName)
    {
        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var symbol = syntaxTree.GetRoot()
            .DescendantNodesAndSelf()
            .OfType<BaseTypeDeclarationSyntax>()
            .Select(x => semanticModel.GetDeclaredSymbol(x)!)
            .First(x => x.Name == symbolName);

        var result = _helper.DoesTypeSymbolImplementInterface(symbol, interfaceName);
        result.Should().BeTrue();
    }

    [Fact]
    public void DoesTypeSymbolImplementInterface_Should_ReturnFalse()
    {
        var source = """
            public class MyClass
            {
            }

            public interface MyInterface
            {
            }
            """;

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<Assembly>());

        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var symbol = syntaxTree.GetRoot()
            .DescendantNodesAndSelf()
            .OfType<BaseTypeDeclarationSyntax>()
            .Select(x => semanticModel.GetDeclaredSymbol(x)!)
            .First(x => x.Name == "MyClass");

        var result = _helper.DoesTypeSymbolImplementInterface(symbol, "MyInterface");
        result.Should().BeFalse();

    }

    [Theory]
    [ClassData(typeof(ClassImplementingInterface))]
    public void DoesClassNodeImplementInterface_Should_ReturnTrue_When_ItDoes(string source, string interfaceFullyQualifiedName)
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
    public void DoesClassNodeImplementInterface_Should_ReturnFalse_When_ThereIsNoBaseList()
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
    public void DoesClassNodeImplementInterface_Should_ReturnFalse_When_NoBasesImplementInterface()
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

public class TypeSymbolImplementingInterface : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var interfaceSource = """
            namespace InterfaceNamespace
            {
                public interface MyInterface
                {
                }
            }

            """;

        var interfaceFullyQualifiedName = "InterfaceNamespace.MyInterface";
        yield return new object[]
        {
            $$"""
            {{interfaceSource}}
            namespace MyNamespace
            {
                public class MyClass : {{interfaceFullyQualifiedName}}
                {
                }
            }
            """,
            "MyClass",
            interfaceFullyQualifiedName
        };

        yield return new object[]
        {
            $$"""
            {{interfaceSource}}
            namespace MyNamespace
            {
                public class MyParentClass : {{interfaceFullyQualifiedName}}
                {
                }

                public class MyClass : MyParentClass
                {
                }
            }
            """,
            "MyClass",
            interfaceFullyQualifiedName
        };

        yield return new object[]
        {
            $$"""
            {{interfaceSource}}
            namespace MyNamespace
            {
                public interface ChildInterface : {{interfaceFullyQualifiedName}}
                {
                }

                public class MyClass : ChildInterface
                {
                }
            }
            """,
            "MyClass",
            interfaceFullyQualifiedName
        };

        yield return new object[]
        {
            $$"""
            {{interfaceSource}}
            namespace MyNamespace
            {
                public interface ChildInterface : {{interfaceFullyQualifiedName}}
                {
                }
            }
            """,
            "ChildInterface",
            interfaceFullyQualifiedName
        };

        yield return new object[]
        {
            interfaceSource,
            "MyInterface",
            interfaceFullyQualifiedName
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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

