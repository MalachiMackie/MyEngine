using System.Numerics;
using FluentAssertions;

namespace MyEngine.Rendering.Tests;
public class LineRenderResourceTests
{
    private readonly LineRenderResource _resource = new();

    [Fact]
    public void RenderLine_Should_EnqueueLine()
    {
        var start = new Vector3(0, 1, 2);
        var end = new Vector3(1, 2, 3);

        _resource.RenderLine(start, end);

        var results = _resource.Flush();

        results.Should().BeEquivalentTo(new[] { new ILineRenderResource.Line(start, end) });
    }

    [Fact]
    public void RenderCircle_Should_EnqueueCorrectCircle()
    {
        var center = new Vector3(0, 0, 0);
        var radius = 5f;

        var result = _resource.RenderLineCircle(center, radius, segments: 3);
        result.IsSuccess.Should().BeTrue();

        var results = _resource.Flush();

        results.Should().BeEquivalentTo(new ILineRenderResource.Line[] {
            new(new Vector3(-4.33f, -2.5f, 1f), new Vector3(0, 5, 1)),
            new(new Vector3(4.33f, -2.5f, 1f), new Vector3(-4.33f, -2.5f, 1)),
            new(new Vector3(0f, 5f, 1f), new Vector3(4.33f, -2.5f, 1f)),
        }, opts => opts.Using<Vector3>(x =>
        {
            x.Subject.X.Should().BeApproximately(x.Expectation.X, 0.01f);
            x.Subject.Y.Should().BeApproximately(x.Expectation.Y, 0.01f);
            x.Subject.Z.Should().BeApproximately(x.Expectation.Z, 0.01f);
        }).WhenTypeIs<Vector3>());
    }

    [Fact]
    public void RenderCircle_Should_ReturnFailure_When_SegmentsIsLessThan3()
    {
        var result = _resource.RenderLineCircle(new Vector3(), 4f, 2);
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void RenderCircle_Should_ReturnFailure_When_RadiusIsLessThanOrEqualTo0(float radius)
    {
        var result = _resource.RenderLineCircle(new Vector3(), radius);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RenderCircle_Should_DefaultTo15Segments()
    {
        var result = _resource.RenderLineCircle(new Vector3(), 1f, segments: null);
        result.IsSuccess.Should().BeTrue();
        var flushed = _resource.Flush();
        flushed.Should().HaveCount(15);
    }
}
