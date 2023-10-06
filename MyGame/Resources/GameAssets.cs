using MyEngine.Assets;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Rendering;
using MyEngine.UI;

namespace MyGame.Resources;

public class GameAssets : IResource
{
    public required Sprite Ball { get; init; }
    public required Sprite White { get; init; }
    public required FontAsset Font { get; init; } 
}
