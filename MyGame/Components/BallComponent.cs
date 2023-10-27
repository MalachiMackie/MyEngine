using MyEngine.Core.Ecs.Components;

namespace MyGame.Components;

public class BallComponent : IComponent
{
    public required bool AttachedToPaddle { get; set; }
}
