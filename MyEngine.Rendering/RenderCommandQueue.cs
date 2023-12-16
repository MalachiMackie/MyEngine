using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Rendering;

public interface IRenderCommandQueue : IResource
{
    public void Enqueue(IRenderCommand command);
    internal IEnumerable<IRenderCommand> Flush();
}


public class RenderCommandQueue : IRenderCommandQueue
{
    private readonly Queue<IRenderCommand> _queue = new();

    public void Enqueue(IRenderCommand command) => _queue.Enqueue(command);

    public IEnumerable<IRenderCommand> Flush()
    {
        while (_queue.TryDequeue(out var command))
        {
            yield return command;
        }
    }
}
