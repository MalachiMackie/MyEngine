using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs.Resources
{
    public class ComponentContainerResource : IResource
    {
        internal Queue<IComponent> NewComponents { get; } = new();

        public void AddComponent(IComponent component)
        {
            NewComponents.Enqueue(component);
        }
    }
}
