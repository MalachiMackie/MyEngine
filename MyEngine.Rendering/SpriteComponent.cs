using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Rendering;

namespace MyEngine.Rendering;

public class SpriteComponent : IComponent
{
    public Sprite Sprite { get; }

    public SpriteComponent(Sprite sprite)
    {
        Sprite = sprite;
    }
}