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
        var entityResult = _entityCommands.CreateEntity(x => x.WithDefaultTransform()
            .WithNoDisplay()
            .WithoutPhysics()
            // todo: detect screen aspect ratio
            .WithComponent(new Camera2DComponent(new Vector2(8f, 6f))));

        if (entityResult.TryGetError(out var addEntityCommandError))
        {
            Console.WriteLine("Failed to add camera: {0}", addEntityCommandError);
        }
    }
}
