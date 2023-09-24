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
        var ballAssetId = _assetCommands.LoadAsset<Texture, TextureLoadData>("ball.png", new TextureLoadData(100));
        _ = _assetCommands.LoadAsset<Texture, TextureLoadData>("silk.png", new TextureLoadData(100));
        var whiteAssetId = _assetCommands.LoadAsset<Texture, TextureLoadData>("White.png", new TextureLoadData(1));
        _resourceRegistrationResource.AddResource(new SpriteAssetIdsResource() { BallAssetId = ballAssetId, WhiteSpriteId = whiteAssetId });
    }
}
