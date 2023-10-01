using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.UI;

namespace MyGame.Resources;

public class FontIdResource : IResource
{
    public required AssetId FontId { get; init; }
}

public class LoadedFontResource : IResource
{
    public required FontAsset Font { get; init; }
}
