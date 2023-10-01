using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.UI;
using MyGame.Resources;

namespace MyGame.Systems;

public class InitUiSystem : ISystem
{

    private readonly FontIdResource _fontResource;
    private readonly LoadedSpritesResource _loadedSpritesResource;
    private readonly ICommands _commands;
    private readonly IHierarchyCommands _hierarchyCommands;
    private readonly AssetCollection _assetCollection;

    public InitUiSystem(FontIdResource fontResource,
        ICommands commands,
        IHierarchyCommands hierarchyCommands,
        AssetCollection assetCollection,
        LoadedSpritesResource loadedSpritesResource)
    {
        _fontResource = fontResource;
        _commands = commands;
        _hierarchyCommands = hierarchyCommands;
        _assetCollection = assetCollection;
        _loadedSpritesResource = loadedSpritesResource;
    }

    private bool _hasFontLoaded;
    private bool _fontLoadFailed;

    public void Run(double deltaTime)
    {
        if (_hasFontLoaded || _fontLoadFailed)
        {
            return;
        }

        var loadFontResult = _assetCollection.TryGetAsset<FontAsset>(_fontResource.FontId);
        if (loadFontResult.IsSuccess)
        {
            _hasFontLoaded = true;
            InitUi(loadFontResult.Unwrap());
            return;
        }

        var loadError = loadFontResult.UnwrapError();
        if (loadError == AssetCollection.GetAssetError.AssetIdNotFound)
        {
            return;
        }

        _fontLoadFailed = true;
        Console.WriteLine("Failed to load font");
    }

    private void InitUi(FontAsset font)
    {
        var canvasEntity = _commands.CreateEntity(new MyEngine.Core.Transform(),
            new UICanvasComponent()).Unwrap();
        var textEntity = _commands.CreateEntity(new MyEngine.Core.Transform(),
            new UITextComponent { Font = font, Text = "HELLO WORLD" },
            new UITransformComponent { Position = new System.Numerics.Vector2() }).Unwrap();
        var boxEntity = _commands.CreateEntity(new MyEngine.Core.Transform(),
            new UIBoxComponent { BackgroundSprite = _loadedSpritesResource.Ball, Dimensions = new System.Numerics.Vector2(100f, 100f) },
            new UITransformComponent { Position = new System.Numerics.Vector2(100f, 100f) }).Unwrap();

        _hierarchyCommands.AddChild(canvasEntity, boxEntity);
        _hierarchyCommands.AddChild(boxEntity, textEntity);
    }
}
