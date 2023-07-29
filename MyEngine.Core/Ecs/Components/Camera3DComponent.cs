namespace MyEngine.Core.Ecs.Components
{
    public class Camera3DComponent : IComponent
    {

        public Camera3DComponent(EntityId entityId)
        {
            EntityId = entityId;
        }

        public EntityId EntityId { get; }
    }
}
