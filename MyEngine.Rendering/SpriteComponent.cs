using MyEngine.Core.Ecs.Components;

namespace MyEngine.Rendering;

public class SpriteComponent : IComponent
{
    public Sprite Sprite { get; }

    public SpriteComponent(Sprite sprite)
    {
        Sprite = sprite;
    }
}
