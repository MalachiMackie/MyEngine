using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;

namespace MyGame.Resources;
public class SpriteAssetIdsResource : IResource
{
    public required AssetId BallAssetId { get; init; }
    public required AssetId WhiteSpriteId { get; init; }
    public required AssetId FontAssetId { get; init; }
}
