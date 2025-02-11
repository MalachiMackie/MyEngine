﻿using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Physics.Bepu;
using MyEngine.Rendering;
using MyEngine.Silk.NET;
using MyEngine.UI;
using MyGame.Systems;

[assembly: AppEntrypoint]

namespace MyGame;

[AppEntrypoint]
public class AppEntrypoint : IAppEntrypoint
{
    public void BuildApp(AppBuilder builder)
    {
        builder
            .AddStartupSystem<AddCameraStartupSystem>()
            .AddSystem<LoadResourcesSystem>(UpdateSystemStage.Instance)
            .AddSystem<AddStartupSpritesSystem>(PostUpdateSystemStage.Instance)
            .AddSystem<QuitOnEscapeSystem>(UpdateSystemStage.Instance)
            .AddSystem<LaunchBallSystem>(UpdateSystemStage.Instance)
            .AddSystem<ResetBallSystem>(UpdateSystemStage.Instance)
            .AddSystem<MovePaddleSystem>(UpdateSystemStage.Instance)
            .AddSystem<BrickCollisionSystem>(UpdateSystemStage.Instance)
            .AddSystem<ToggleColliderDebugDisplaySystem>(UpdateSystemStage.Instance)
            .AddSystem<InitUiSystem>(UpdateSystemStage.Instance)
            .AddPlugin(new CorePlugin())
            .AddPlugin(new BepuPhysicsPlugin())
            .AddPlugin(new SilkPlugin())
            .AddPlugin(new RenderPlugin("My Game", 800, 600))
            .AddPlugin(new AssetPlugin())
            .AddPlugin(new UIPlugin());
    }
}
