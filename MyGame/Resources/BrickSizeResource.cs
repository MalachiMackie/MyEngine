using System.Numerics;
using MyEngine.Core.Ecs.Resources;

namespace MyGame.Resources;

public class BrickSizeResource : IResource
{
    public required Vector2 Dimensions { get; init; }
}
