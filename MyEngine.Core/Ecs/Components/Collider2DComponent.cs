namespace MyEngine.Core.Ecs.Components
{
    public class Collider2DComponent : IComponent
    {
        public BoxCollider2D? BoxCollider { get; }
        public CircleCollider2D? CircleCollider { get; }

        public ICollider2D Collider { get; }

        public EntityId EntityId { get; }

        public Collider2DComponent(EntityId entityId, BoxCollider2D boxCollider)
        {
            EntityId = entityId;
            BoxCollider = boxCollider;
            Collider = BoxCollider;
        }

        public Collider2DComponent(EntityId entityId, CircleCollider2D circleCollider)
        {
            EntityId = entityId;
            CircleCollider = circleCollider;
            Collider = CircleCollider;
        }
    }
}
