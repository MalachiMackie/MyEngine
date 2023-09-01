using MyEngine.Core.Ecs.Components;

namespace MyEngine.Physics;

public class PhysicsMaterial : IComponent
{
    public float Bounciness { get; set; }

    public PhysicsMaterial(float bounciness)
    {
        Bounciness = Math.Clamp(bounciness, 0f, 1f);
    }

}
