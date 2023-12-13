using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Input;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyEngine.Silk.NET;
using MyEngine.TestScenarios.Physics.Bouncing.DynamicCollisions;
using MyEngine.TestScenarios.Physics.Bouncing.StaticCollisions;
using MyEngine.UI;

namespace MyEngine.TestScenarios;

//[AppEntrypoint]
public class AppEntrpoint : IAppEntrypoint
{
    private bool _dynamicToDynamic = true; 

    public void BuildApp(AppBuilder builder)
    {
        builder.AddPlugin(new CorePlugin())
            .AddPlugin(new Physics2DPlugin())
            .AddPlugin(new RenderPlugin("Engine Test Scenarios", 800, 600))
            .AddPlugin(new SilkPlugin())
            .AddPlugin(new AssetPlugin())
            .AddPlugin(new UIPlugin());

        if (_dynamicToDynamic)
        {
            DynamicToDynamicBouncingScenario.Register(builder);
        }
        else
        {
            DynamicToStaticBouncingScenario.Register(builder);
        }

    }
}
