namespace MyEngine.Core.Ecs.Components;

public class KinematicBody2DComponent : IComponent
{
    public Vector2 Velocity { get; set; }

    // todo: angular velocity
}
