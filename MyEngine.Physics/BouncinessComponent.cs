using MyEngine.Core.Ecs.Components;

namespace MyEngine.Physics;

public class BouncinessComponent : IComponent
{
    public BouncinessComponent(float bounciness)
    {
        Bounciness = bounciness;
    }

    public float Bounciness { get; set; }
}
