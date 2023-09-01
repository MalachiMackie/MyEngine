using MyEngine.Core;

namespace MyEngine.Physics;

public class Physics2DPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddSystem<PhysicsSystem>()
            .AddSystem<ColliderDebugDisplaySystem>()
            .AddResource(new DebugColliderDisplayResource())
            .AddResource(new MyPhysics())
            .AddResource(new CollisionsResource())
            .AddResource(new PhysicsResource());
    }
}
