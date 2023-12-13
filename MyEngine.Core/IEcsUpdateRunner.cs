using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Core;

public interface IEcsUpdateRunner : IResource
{
    public void Run();

    public void AddUpdateHandler(Action<double> handler);
}
