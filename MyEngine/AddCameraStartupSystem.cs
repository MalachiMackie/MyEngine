using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System.Numerics;

namespace MyEngine.Runtime;

internal class AddCameraStartupSystem : IStartupSystem
{
    private readonly ICommands _entityCommands;

    public AddCameraStartupSystem(
        ICommands entityContainer)
    {
        _entityCommands = entityContainer;
    }

    public void Run()
    {
        var entity = _entityCommands.AddEntity(x => x.WithDefaultTransform()
            .WithComponent(new Camera2DComponent(new Vector2(8f, 4.5f))));
    }
}
