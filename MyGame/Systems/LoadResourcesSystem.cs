using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using MyEngine.UI;
using MyGame.Resources;

namespace MyGame.Systems;

public class LoadResourcesSystem : ISystem
{
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly IAssetCommands _assetCommands;
    private readonly IAssetCollection _assetCollection;

    public LoadResourcesSystem(
        ResourceRegistrationResource resourceRegistrationResource,
        IAssetCommands assetCommands,
        IAssetCollection assetCollection)
    {
        _resourceRegistrationResource = resourceRegistrationResource;
        _assetCommands = assetCommands;
        _assetCollection = assetCollection;
    }

    private AssetId BallAssetId { get; set; } = null!;
    private AssetId WhiteAssetId { get; set; } = null!;
    private AssetId FontAssetId { get; set; } = null!;

    enum State
    {
        NotStarted,
        LoadingInitiated,
        StillLoading,
        LoadingFinished
    }

    private State _state;

    public void Run(double _)
    {
        switch (_state)
        {
            case State.NotStarted:
                InitialLoad();
                _state = State.LoadingInitiated;
                break;
            case State.StillLoading:
            case State.LoadingInitiated:
                _state = CheckLoad();
                break;
        }
    }

    private void InitialLoad()
    {
        BallAssetId = _assetCommands.LoadAsset<Sprite, CreateSpriteFromFullTexture>("ball.png", new CreateSpriteFromFullTexture(100, SpriteOrigin.Center));
        WhiteAssetId = _assetCommands.LoadAsset<Sprite, CreateSpriteFromFullTexture>("White.png", new CreateSpriteFromFullTexture(1, SpriteOrigin.Center));
        FontAssetId = _assetCommands.LoadAsset<FontAsset>("Hermit-Regular-fed68123.png");
    }

    private State CheckLoad()
    {
        var whiteResult = _assetCollection.TryGetAsset<Sprite>(WhiteAssetId);
        var ballResult = _assetCollection.TryGetAsset<Sprite>(BallAssetId);
        var fontResult = _assetCollection.TryGetAsset<FontAsset>(FontAssetId);

        if (whiteResult.IsFailure || ballResult.IsFailure || fontResult.IsFailure)
        {
            Console.WriteLine("Loading Game Assets Failed");
            return State.LoadingFinished;
        }
        if (whiteResult.TryGetValue(out var white) && white is not null
            && ballResult.TryGetValue(out var ball) && ball is not null
            && fontResult.TryGetValue(out var font) && font is not null)
        {
            _resourceRegistrationResource.AddResource(new GameAssets
            {
                Ball = ball,
                Font = font,
                White = white,
            });
            return State.LoadingFinished;
        }

        return State.StillLoading;
    }
}
