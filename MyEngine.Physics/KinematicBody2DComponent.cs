using System.Numerics;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Physics;

public class KinematicBody2DComponent : IComponent
{
    private Vector2 _velocity;
    public Vector2 Velocity
    {
        get => _velocity;
        set
        {
            _velocity = value;
            Dirty = true;
        }
    }

    // todo: angular velocity

    internal bool Dirty { get; set; }
}
