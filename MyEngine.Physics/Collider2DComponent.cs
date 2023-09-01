using MyEngine.Core.Ecs.Components;

namespace MyEngine.Physics;

public class Collider2DComponent : IComponent
{
    public BoxCollider2D? BoxCollider { get; }
    public CircleCollider2D? CircleCollider { get; }

    public ICollider2D Collider { get; }

    public Collider2DComponent(BoxCollider2D boxCollider)
    {
        BoxCollider = boxCollider;
        Collider = BoxCollider;
    }

    public Collider2DComponent(CircleCollider2D circleCollider)
    {
        CircleCollider = circleCollider;
        Collider = CircleCollider;
    }
}
