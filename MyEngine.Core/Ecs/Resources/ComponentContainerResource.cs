using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs.Resources
{
    internal class ComponentContainerResource<T> : IResource
        where T : IComponent
    {
        internal Queue<T> NewComponents { get; } = new();

        public void AddComponent(T component)
        {
            NewComponents.Enqueue(component);
        }
    }
}
