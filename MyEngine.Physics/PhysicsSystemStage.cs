using MyEngine.Core;

namespace MyEngine.Physics;

public class PhysicsSystemStage : ISystemStage
{
    public static PhysicsSystemStage Instance { get; } = new PhysicsSystemStage();

    private PhysicsSystemStage()
    {

    }

    public bool Equals(ISystemStage? other)
    {
        return other is PhysicsSystemStage;
    }
}
