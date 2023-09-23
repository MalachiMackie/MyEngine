using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using MyGame.Resources;

namespace MyGame.Systems;

public class LoadSpritesSystem : IStartupSystem
{
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly IAssetCommands _assetCommands;

    public LoadSpritesSystem(
        ResourceRegistrationResource resourceRegistrationResource,
        IAssetCommands assetCommands)
    {
        _resourceRegistrationResource = resourceRegistrationResource;
        _assetCommands = assetCommands;
    }

    public void Run()
    {
        var silkAssetId = _assetCommands.LoadAsset<Sprite>("silk.png");
        var whiteAssetId = _assetCommands.LoadAsset<Sprite>("White.png");
        _resourceRegistrationResource.AddResource(new SpriteAssetIdsResource() { SilkSpriteId = silkAssetId, WhiteSpriteId = whiteAssetId });
    }
}
