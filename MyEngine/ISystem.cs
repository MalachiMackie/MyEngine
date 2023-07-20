namespace MyEngine
{
    internal interface ISystem<TComponent>
        where TComponent : IComponent
    {
        void Run(double deltaTime, TComponent component);
    }

    internal interface ISystem<TComponent1, TComponent2>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
    {
        void Run(double deltaTime, TComponent1 component1, TComponent2 component2);
    }

    internal interface ISystem<TComponent1, TComponent2, TResource>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
        where TResource : IResource
    {
        void Run(double deltaTime, TComponent1 component1, TComponent2 component2, TResource resource);
    }

    internal interface ISystem<TComponent1, TComponent2, TResource1, TResource2>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
        where TResource1 : IResource
        where TResource2 : IResource
    {
        void Run(double deltaTime, TComponent1 component1, TComponent2 component2, TResource1 resource1, TResource2 resource2);
    }
}
