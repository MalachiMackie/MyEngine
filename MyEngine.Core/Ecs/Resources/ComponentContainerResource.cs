using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs.Resources
{
    public class ComponentContainerResource : IResource
    {
        internal Queue<IComponent> NewComponents { get; } = new();
        internal Queue<(EntityId EntityId, Type ComponentType)> RemoveComponents { get; } = new();

        public void AddComponent(IComponent component)
        {
            NewComponents.Enqueue(component);
        }

        public void RemoveComponent<T>(EntityId entityId) where T : IComponent
        {
            RemoveComponents.Enqueue((entityId, typeof(T)));
        }
    }
}
