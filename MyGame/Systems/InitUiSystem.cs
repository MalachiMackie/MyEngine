using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.UI;
using MyGame.Resources;

namespace MyGame.Systems;

public class InitUiSystem : ISystem
{

    private readonly GameAssets _gameAssets;
    private readonly ICommands _commands;
    private readonly IHierarchyCommands _hierarchyCommands;
    private readonly AssetCollection _assetCollection;

    public InitUiSystem(ICommands commands,
        IHierarchyCommands hierarchyCommands,
        AssetCollection assetCollection,
        GameAssets gameAssets)
    {
        _commands = commands;
        _hierarchyCommands = hierarchyCommands;
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

        var canvasEntity = _commands.CreateEntity(new Transform(),
            new UICanvasComponent()).Unwrap();
        var textEntity = _commands.CreateEntity(new Transform(),
            new UITextComponent { Font = _gameAssets.Font, Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\r\nA B C D E F G H I" },
            new UITransparencyComponent { Transparency = 0.3f },
            new UITransformComponent { Position = new Vector3() }).Unwrap();
        var boxEntity1 = _commands.CreateEntity(new Transform(),
            new UIBoxComponent { BackgroundSprite = _gameAssets.White, Dimensions = new Vector2(100f, 100f) },
            new UITransformComponent { Position = new Vector3(100f, 100f, 0f) }).Unwrap();
        var boxEntity2 = _commands.CreateEntity(new Transform(),
            new UIBoxComponent { BackgroundSprite = _gameAssets.White, Dimensions = new Vector2(50f, 50f) },
            new UITransparencyComponent { Transparency = 0.3f },
            new UITransformComponent { Position = new Vector3(125f, 0f, 0f) }).Unwrap();

        _hierarchyCommands.AddChild(canvasEntity, boxEntity1);
        _hierarchyCommands.AddChild(boxEntity1, textEntity);
        _hierarchyCommands.AddChild(textEntity, boxEntity2);
        _initialized = true;
    }
}
