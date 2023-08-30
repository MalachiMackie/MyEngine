namespace MyEngine.Core.Ecs.Components;

public class SpriteComponent : IComponent
{
    public Sprite Sprite { get; }

    public SpriteComponent(Sprite sprite)
    {
        Sprite = sprite;
    }
}
