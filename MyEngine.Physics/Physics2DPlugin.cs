using MyEngine.Core;

namespace MyEngine.Physics;

public class Physics2DPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddSystemStage(PhysicsSystemStage.Instance, 0)
            .AddSystem<PhysicsSystem>(PhysicsSystemStage.Instance)
            .AddSystem<ColliderDebugDisplaySystem>(PostUpdateSystemStage.Instance)
            .AddSystem<BounceSystem>(PhysicsSystemStage.Instance) // todo: make sure this runs directly after PhysicsSystem
            .AddResource(new DebugColliderDisplayResource())
            .AddResource(new BepuPhysicsAdapter())
            .AddResource(new CollisionsResource())
            .AddResource(new PhysicsResource());
    }
}
