using MyEngine.Core.Ecs.Components;

namespace MyGame.Components;

public class LogPositionComponent : IComponent
{
    public required string Name { get; init; }
}
