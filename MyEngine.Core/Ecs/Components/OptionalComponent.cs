namespace MyEngine.Core.Ecs.Components
{
    public class OptionalComponent<T> : IComponent
        where T : IComponent
    {
        public T? Component { get; }

        public OptionalComponent(T? component) 
        {
            Component = component;
        }
    }
}
