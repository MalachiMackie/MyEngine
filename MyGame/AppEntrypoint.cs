using MyEngine.Core;
using MyEngine.Input;
using MyEngine.Physics;
using MyEngine.Rendering;
using MyGame.Systems;

namespace MyGame;

public class AppEntrypoint : IAppEntrypoint
{
    public void BuildApp(AppBuilder builder)
    {
        builder.AddStartupSystem<AddStartupSpritesSystem>()
            .AddPlugin(new Physics2DPlugin())
            .AddPlugin(new InputPlugin())
            .AddPlugin(new RenderPlugin("My Game", 800, 600));
    }
}
