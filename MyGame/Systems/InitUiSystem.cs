using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.UI;
using MyGame.Resources;

namespace MyGame.Systems;

public class InitUiSystem : ISystem
{

    private readonly GameAssets _gameAssets;
    private readonly ICommands _commands;
    private readonly AssetCollection _assetCollection;

    public InitUiSystem(ICommands commands,
        AssetCollection assetCollection,
        GameAssets gameAssets)
    {
        _commands = commands;
        _assetCollection = assetCollection;
        _gameAssets = gameAssets;
    }

    private bool _initialized; 

    public void Run(double deltaTime)
    {
        if (_initialized)
        {
            return;
        }

        _commands.CreateEntity(x => x
            .WithDefaultTransform()
            .WithComponent(new UICanvasComponent())
            .WithChild(y => y
                .WithDefaultTransform()
                .WithComponents(
                    new UIBoxComponent { BackgroundSprite = _gameAssets.White, Dimensions = new Vector2(100f, 100f) },
                    new UITransformComponent { Position = new Vector3(100f, 100f, 0f) })
                .WithChild(z => z
                    .WithDefaultTransform()
                    .WithComponents(
                        new UITextComponent { Font = _gameAssets.Font, Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\r\nA B C D E F G H I"},
                        new UITransparencyComponent { Transparency = 0.3f },
                        new UITransformComponent { Position = new Vector3() })
                    .WithChild(w => w
                        .WithDefaultTransform()
                        .WithComponents(
                        new UIBoxComponent { BackgroundSprite = _gameAssets.White, Dimensions = new Vector2(50f, 50f) },
                        new UITransparencyComponent { Transparency = 0.3f },
                        new UITransformComponent { Position = new Vector3(125f, 0f, 0f) })))));

        _initialized = true;
    }
}
