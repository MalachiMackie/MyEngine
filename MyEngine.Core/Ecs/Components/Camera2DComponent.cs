namespace MyEngine.Core.Ecs.Components
{
    public class Camera2DComponent : IComponent
    {
        public EntityId EntityId { get; }

        public Vector2 Size { get; set; }

        public Camera2DComponent(EntityId entityId, Vector2 size)
        {
            EntityId = entityId;
            Size = size;
        }
    }
}
