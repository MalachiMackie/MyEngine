using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyGame.Utils;

public record Brick(EntityId EntityId, IReadOnlyCollection<IComponent> Components);

public static class BrickBuilder
{
    public static Brick BuildBrick(IEntityCommands entityCommands, Vector2 position, float width, float height)
    {
        var entity = entityCommands.AddEntity(TransformComponent.DefaultTransform(position: position.Extend(3.0f), scale: new Vector3(width, height, 1f)));
        var components = new List<IComponent>
        {
            new SpriteComponent(),
            new StaticBody2DComponent(),
            new Collider2DComponent(new BoxCollider2D(Vector2.One))
        };

        return new Brick(entity, components);
    }
}
