using MyEngine.Core;

namespace MyEngine.Physics.Bepu;

public class BepuPhysicsPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddPlugin(new Physics2DPlugin())
            .AddResource<IPhysicsAdapter>(new BepuPhysicsAdapter());
    }
}
