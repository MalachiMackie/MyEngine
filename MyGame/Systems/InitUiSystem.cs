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

        var canvasEntity = _commands.CreateEntity(new MyEngine.Core.Transform(),
            new UICanvasComponent()).Unwrap();
        var textEntity = _commands.CreateEntity(new MyEngine.Core.Transform(),
            new UITextComponent { Font = _gameAssets.Font, Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\r\nA B C D E F G H I", Transparency = 0.5f },
            new UITransformComponent { Position = new System.Numerics.Vector2() }).Unwrap();
        var boxEntity = _commands.CreateEntity(new MyEngine.Core.Transform(),
            new UIBoxComponent { BackgroundSprite = _gameAssets.White, Dimensions = new System.Numerics.Vector2(100f, 100f), Transparency = 1f },
            new UITransformComponent { Position = new System.Numerics.Vector2(100f, 100f) }).Unwrap();

        _hierarchyCommands.AddChild(canvasEntity, boxEntity);
        _hierarchyCommands.AddChild(boxEntity, textEntity);
        _initialized = true;
    }
}
