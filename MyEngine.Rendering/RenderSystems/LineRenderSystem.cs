using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Rendering;

public class LineRenderSystem : ISystem
{
    private readonly RenderCommandQueue _commandQueue;
    private readonly ILineRenderResource _lineRenderResource;

    public LineRenderSystem(RenderCommandQueue commandQueue, ILineRenderResource lineRenderResource)
    {
        _commandQueue = commandQueue;
        _lineRenderResource = lineRenderResource;
    }

    public void Run(double deltaTime)
    {
        var lines = _lineRenderResource.Flush();
        foreach (var line in lines)
        {
            _commandQueue.Enqueue(new RenderLineCommand(line.Start, line.End));
        }
    }
}
