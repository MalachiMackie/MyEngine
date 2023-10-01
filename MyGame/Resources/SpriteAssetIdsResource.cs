using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Rendering;

namespace MyGame.Resources;
public class SpriteAssetIdsResource : IResource
{
    public required AssetId BallAssetId { get; init; }
    public required AssetId WhiteSpriteId { get; init; }
}

public class LoadedSpritesResource : IResource
{
    public required Sprite Ball { get; init; }
    public required Sprite White { get; init; }
}
