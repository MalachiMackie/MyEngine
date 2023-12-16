using System.Numerics;
using FakeItEasy;

namespace MyEngine.Rendering.Tests;

public class LineRenderSystemTests
{
    private readonly IRenderCommandQueue _commandQueue = A.Fake<IRenderCommandQueue>();
    private readonly ILineRenderResource _lineRenderResource = A.Fake<ILineRenderResource>();
    private readonly LineRenderSystem _system;

    public LineRenderSystemTests()
    {
        _system = new LineRenderSystem(_commandQueue, _lineRenderResource);
    }

    [Fact]
    public void Run_Should_EnqueueAllLineRenders()
    {
        var start = new Vector3(0, 1, 2);
        var end = new Vector3(1, 2, 3);
        A.CallTo(() => _lineRenderResource.Flush()).Returns(new[] { new ILineRenderResource.Line(start, end) });
        _system.Run(1);

        A.CallTo(() => _commandQueue.Enqueue(new RenderLineCommand(start, end))).MustHaveHappened();
    }
}