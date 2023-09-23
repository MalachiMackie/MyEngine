using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorHelperTests;

public class DoesTypeSymbolImplementInterface
{
    private readonly SourceGeneratorHelpers _helper = new();

    [Theory]
    [ClassData(typeof(TypeSymbolImplementingInterface))]
    public void Should_ReturnTrue_When_ItDoes(string source, string symbolName, string interfaceName)
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
    public void ymbolImplementInterface_Should_ReturnFalse()
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
