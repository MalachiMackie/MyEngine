namespace MyEngine.SourceGenerator.Tests;

public class TemplateTests
{
    [Fact]
    public void Should_ReplaceTemplateAtCorrectIndentation_When_ReplacementHasMultipleLines()
    {
        var template = SourceTemplate.Load(
"""

    {template:MyTemplate}

""");

        var replacement =
"""
Hi
Hi
    Hi
""";

        template.SubstitutePart("MyTemplate", replacement);
        var result = template.Build();

        result.Should().Be(
"""

    Hi
    Hi
        Hi

""");
    }

    [Fact]
    public void Should_ReplaceTemplateAtCorrectIndentation_When_ReplacementHasMultipleLines_And_TemplateIsOnTheFirstLine()
    {
        var template = SourceTemplate.Load(
        "someTextBeforeThe{template:MyTemplate}");

        var replacement =
"""
Hi
Hi
    Hi
""";

        template.SubstitutePart("MyTemplate", replacement);
        var result = template.Build();

        result.Should().Be(
"""
someTextBeforeTheHi
                 Hi
                     Hi
""");

    }

    [Fact]
    public void Should_ReplaceMultipleInstancesOfTheSameTemplateAtDifferentIndentationLevels()
    {
        var template = SourceTemplate.Load(
            """
                {template:MyTemplate}
                    {template:MyTemplate}
            """);

        var replacement = "Hi\r\nHi";

        template.SubstitutePart("MyTemplate", replacement);
        var result = template.Build();
        result.Should().Be(
"""
    Hi
    Hi
        Hi
        Hi
""");
    }
}
