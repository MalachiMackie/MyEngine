using MyEngine.Core.Ecs.Components;

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

    public interface IRenderSystem<TComponent1, TComponent2>
        where TComponent1 : IComponent
        where TComponent2 : IComponent
    {
        void Render(double deltaTime, TComponent1 component1, TComponent2 component2);
    }
}
