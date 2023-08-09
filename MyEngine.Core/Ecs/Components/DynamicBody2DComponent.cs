namespace MyEngine.Core.Ecs.Components
{
    public class DynamicBody2DComponent : IComponent
    {
        public EntityId EntityId { get; }

        public DynamicBody2DComponent(EntityId entityId)
        {
            EntityId = entityId;
        }
    }
}
