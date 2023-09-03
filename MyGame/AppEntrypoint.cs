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
            .AddStartupSystem<AddCameraStartupSystem>()
            .AddSystem<QuitOnEscapeSystem>(UpdateSystemStage.Instance)
            .AddSystem<LaunchBallSystem>(UpdateSystemStage.Instance)
            .AddSystem<KinematicBounceSystem>(UpdateSystemStage.Instance)
            .AddSystem<ResetBallSystem>(UpdateSystemStage.Instance)
            .AddSystem<MovePaddleSystem>(UpdateSystemStage.Instance)
            .AddSystem<BrickCollisionSystem>(UpdateSystemStage.Instance)
            .AddSystem<ToggleColliderDebugDisplaySystem>(UpdateSystemStage.Instance)
            .AddPlugin(new CorePlugin())
            .AddPlugin(new Physics2DPlugin())
            .AddPlugin(new InputPlugin())
            .AddPlugin(new RenderPlugin("My Game", 800, 600))
            ;
    }
}
