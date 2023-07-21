using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Core.Ecs.Systems
{
    // todo: find a way to have different combinations of Components and Resources
    public interface ISystem
    {
        void Run(double deltaTime);
    }

    public interface ISystem<TComponent>
        where TComponent : IComponent
    {
        void Run(double deltaTime, TComponent component);
    }

    public interface ISystem<TComponent1, TComponent2>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
    {
        void Run(double deltaTime, TComponent1 component1, TComponent2 component2);
    }

    public interface ISystem<TComponent1, TComponent2, TResource>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
        where TResource : IResource
    {
        void Run(double deltaTime, TComponent1 component1, TComponent2 component2, TResource resource);
    }

    public interface ISystem<TComponent1, TComponent2, TResource1, TResource2>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
        where TResource1 : IResource
        where TResource2 : IResource
    {
        void Run(double deltaTime, TComponent1 component1, TComponent2 component2, TResource1 resource1, TResource2 resource2);
    }
}
