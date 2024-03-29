using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyEngine.Core;
using MyEngine.SourceGenerator.Tests.SourceGeneratorTests;

namespace MyEngine.SourceGenerator.Tests.SourceGeneratorHelperTests;
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

    public enum AttributeType
    {
        Assembly,
        Class,
        Field
    }

    [Theory]
    [InlineData("MyEngine.Core.AppEntrypointAttribute", EngineAttribute.AppEntrypoint, AttributeType.Class)]
    public void DoesAttributeMatch_Should_ReturnTrue(string attributeFullyQualifiedName, EngineAttribute attribute, AttributeType attributeType)
    {
        string source;
        if (attributeType == AttributeType.Assembly)
        {
            source = $"""
                [assembly: {attributeFullyQualifiedName}]
                """;
        }
        else if (attributeType == AttributeType.Class)
        {
            source = $$"""
                [{{attributeFullyQualifiedName}}]
                public class MyClass{}
                """;
        }
        else if (attributeType == AttributeType.Field)
        {
            source = $$"""
            public class MyClass
            {
                [{{attributeFullyQualifiedName}}]
                public string _myField;
            }
            """;
        }
        else
        {
            throw new InvalidOperationException();
        }

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            new[] { typeof(AppEntrypointAttribute).Assembly });

        AttributeData attributeData;
        if (attributeType == AttributeType.Assembly)
        {
            attributeData = compilation.Assembly.GetAttributes().First(x => x.AttributeClass!.ToDisplayString() == attributeFullyQualifiedName);
        }
        else if (attributeType == AttributeType.Class)
        {
            attributeData = compilation.GetTypeByMetadataName("MyClass")!.GetAttributes().Single();
        }
        else if (attributeType == AttributeType.Field)
        {
            attributeData = compilation.GetTypeByMetadataName("MyClass")!
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Single()
                .GetAttributes()
                .Single();
        }
        else
        {
            throw new InvalidOperationException();
        }

        var result = _helper.DoesAttributeMatch(attributeData, attribute);
        result.Should().BeTrue();
    }

    [Fact]
    public void DoesAttributeMatch_Should_ReturnFalse_When_ItDoesNot()
    {
        var source = """
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public class CustomAttribute : System.Attribute
            {
            }

            [Custom]
            public class MyClass
            {
            }
            """;

        var compilation = SourceGeneratorTestHelpers.CreateCompilation(
            source,
            Array.Empty<KeyValuePair<string, string>>(),
            new[] { typeof(AppEntrypointAttribute).Assembly });

        var syntaxTree = compilation.SyntaxTrees.Single();

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var attributeData = syntaxTree.GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .Select(x => semanticModel.GetDeclaredSymbol(x)!)
            .First(x => x.Name == "MyClass")
            .GetAttributes()
            .Single();

        var result = _helper.DoesAttributeMatch(attributeData, EngineAttribute.AppEntrypoint);
        result.Should().BeFalse();
    }
}
