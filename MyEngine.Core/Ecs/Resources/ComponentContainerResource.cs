using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs.Resources
{
    internal class ComponentContainerResource : IResource
    {
        internal Queue<IComponent> NewComponents { get; } = new();

        public void AddComponent(IComponent component)
        {
            NewComponents.Enqueue(component);
        }
    }
}
