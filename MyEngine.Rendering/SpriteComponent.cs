using System.Numerics;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Rendering;

namespace MyEngine.Rendering;

public class SpriteComponent : IComponent
{
    public Sprite Sprite { get; }

    public Vector2 Dimensions { get; }

    public SpriteComponent(Sprite sprite)
    {
        Sprite = sprite;
        Dimensions = sprite.WorldDimensions;
    }

    public SpriteComponent(Sprite sprite, Vector2 dimensions)
    {
        Sprite = sprite;
        Dimensions = dimensions;
    }
}