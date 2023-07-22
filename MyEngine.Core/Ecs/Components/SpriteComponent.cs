namespace MyEngine.Core.Ecs.Components
{
    public class SpriteComponent : IComponent
    {
        public SpriteComponent(EntityId entityId)
        {
            EntityId = entityId;
        }

        public EntityId EntityId { get; }
    }
}
