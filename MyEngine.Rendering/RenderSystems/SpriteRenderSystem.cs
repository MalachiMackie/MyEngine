using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Rendering;

namespace MyEngine.Rendering.RenderSystems;

public class SpriteRenderSystem : ISystem
{
    private IQuery<SpriteComponent, TransformComponent> _query;
    private readonly RenderCommandQueue _commandQueue;

    public SpriteRenderSystem(RenderCommandQueue commandQueue, IQuery<SpriteComponent, TransformComponent> query)
    {
        _commandQueue = commandQueue;
        _query = query;
    }

    public void Run(double deltaTime)
    {
        foreach (var (sprite, transform) in _query)
        {
            _commandQueue.Enqueue(new RenderSpriteCommand(sprite.Sprite, transform.GlobalTransform));
        }
    }
}
