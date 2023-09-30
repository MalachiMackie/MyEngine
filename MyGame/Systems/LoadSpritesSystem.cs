using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Rendering;
using MyEngine.UI;
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
        var ballAssetId = _assetCommands.LoadAsset<Sprite, CreateSpriteFromFullTexture>("ball.png", new CreateSpriteFromFullTexture(100));
        var whiteAssetId = _assetCommands.LoadAsset<Sprite, CreateSpriteFromFullTexture>("White.png", new CreateSpriteFromFullTexture(1));
        _resourceRegistrationResource.AddResource(new SpriteAssetIdsResource()
        {
            BallAssetId = ballAssetId,
            WhiteSpriteId = whiteAssetId,
        });
    }
}
