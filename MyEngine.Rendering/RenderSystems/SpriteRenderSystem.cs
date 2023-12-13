using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Rendering.RenderSystems;

public class SpriteRenderSystem : ISystem
{
    private readonly IQuery<SpriteComponent, TransformComponent, OptionalComponent<TransparencyComponent>> _query;
    private readonly RenderCommandQueue _commandQueue;

    public SpriteRenderSystem(RenderCommandQueue commandQueue, IQuery<SpriteComponent, TransformComponent, OptionalComponent<TransparencyComponent>> query)
    {
        _commandQueue = commandQueue;
        _query = query;
    }

    public void Run(double deltaTime)
    {
        foreach (var (sprite, transform, maybeTransparency) in _query)
        {
            _commandQueue.Enqueue(new RenderSpriteCommand(sprite.Sprite, sprite.Dimensions, transform.GlobalTransform, maybeTransparency.Component?.Transparency ?? 1f));
        }
    }
}
