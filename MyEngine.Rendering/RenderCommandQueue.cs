using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Rendering;

public class RenderCommandQueue : IResource
{
    private readonly Queue<IRenderCommand> _queue = new();

    public void Enqueue(IRenderCommand command) => _queue.Enqueue(command);

    internal IEnumerable<IRenderCommand> Flush()
    {
        while (_queue.TryDequeue(out var command))
        {
            yield return command;
        }
    }
}
