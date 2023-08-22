using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System.Numerics;

namespace MyEngine.Runtime;

internal class AddCameraStartupSystem : IStartupSystem
{
    private readonly ComponentContainerResource _componentContainer;
    private readonly IEntityCommands _entityCommands;

    public AddCameraStartupSystem(
        ComponentContainerResource componentContainer,
        IEntityCommands entityContainer)
    {
        _componentContainer = componentContainer;
        _entityCommands = entityContainer;
    }

    public void Run()
    {
        var entity = _entityCommands.AddEntity();
        _componentContainer.AddComponent(entity, new Camera2DComponent(new Vector2(8f, 4.5f)));
    }
}
