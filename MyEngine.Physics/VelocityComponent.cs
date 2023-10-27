using System.Numerics;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Physics;

public class VelocityComponent : IComponent
{
    // todo: Set velocity to 0 for kinematic body if VelocityComponent has been removed
    public Vector3 Velocity { get; internal set; }
}
