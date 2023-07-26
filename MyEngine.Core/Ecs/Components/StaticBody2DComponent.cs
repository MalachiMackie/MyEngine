namespace MyEngine.Core.Ecs.Components
{
    public class StaticBody2DComponent : IComponent
    {
        public StaticBody2DComponent(
            EntityId entityId)
        {
            EntityId = entityId;
        }

        public EntityId EntityId { get; }
    }
}
