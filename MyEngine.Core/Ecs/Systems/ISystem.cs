using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs.Systems
{
    public interface ISystem
    {
        void Run(double deltaTime);
    }

    public interface IRenderSystem
    {
        void Render(double deltaTime);
    }
}
