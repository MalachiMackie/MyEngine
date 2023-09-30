using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.UI;

namespace MyGame.Resources;

public class FontResource : IResource
{
    public required AssetId FontId { get; init; }
}
