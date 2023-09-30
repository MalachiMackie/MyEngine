using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.UI;
using MyGame.Resources;

namespace MyGame.Systems;

public class InitUiSystem : ISystem
{

    private readonly FontResource _fontResource;
    private readonly ICommands _commands;
    private readonly IHierarchyCommands _hierarchyCommands;
    private readonly AssetCollection _assetCollection;

    public InitUiSystem(FontResource fontResource, ICommands commands, IHierarchyCommands hierarchyCommands, AssetCollection assetCollection)
    {
        _fontResource = fontResource;
        _commands = commands;
        _hierarchyCommands = hierarchyCommands;
        _assetCollection = assetCollection;
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

        _hierarchyCommands.AddChild(canvasEntity, textEntity);
    }
}
