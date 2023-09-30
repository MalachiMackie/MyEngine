using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Rendering;

namespace MyEngine.UI;

public class UICanvasComponent : IComponent
{
}

public class UIBoxComponent : IComponent
{
    public required Vector2 Dimensions { get; set; }
    public required Sprite BackgroundSprite { get; set; }
}

public class UITextComponent : IComponent
{
    public required string Text { get; set; }
    public required FontAsset Font { get; set; }
}

public class UITransformComponent : IComponent
{
    public required Vector2 Position { get; set; }
}
