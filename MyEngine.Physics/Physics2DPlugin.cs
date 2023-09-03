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
            .AddResource(new DebugColliderDisplayResource())
            .AddResource(new MyPhysics())
            .AddResource(new CollisionsResource())
            .AddResource(new PhysicsResource());
    }
}
