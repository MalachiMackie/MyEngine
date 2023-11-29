using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using System.Numerics;

namespace MyGame;

public class AddCameraStartupSystem : IStartupSystem
{
    private readonly ICommands _entityCommands;

    public AddCameraStartupSystem(
        ICommands entityContainer)
    {
        _entityCommands = entityContainer;
    }

    public void Run()
    {
        // todo: detect screen aspect ratio
        var entityResult = _entityCommands.CreateEntity(new MyEngine.Core.Transform(), new IComponent[] { new Camera2DComponent(new Vector2(8f, 6f)) });

        if (entityResult.TryGetErrors(out var addEntityCommandError))
        {
            Console.WriteLine("Failed to add camera: {0}", string.Join(";", addEntityCommandError));
        }
    }
}
