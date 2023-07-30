namespace MyEngine.Core.Ecs.Components
{
    public class OptionalComponent<T> : IComponent
        where T : IComponent
    {
        public EntityId EntityId { get; }

        public T? Component { get; }

        public OptionalComponent(EntityId entityId, T? component) 
        {
            EntityId = entityId;
            Component = component;
        }
    }
}
