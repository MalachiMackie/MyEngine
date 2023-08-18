using System.Numerics;
using MyEngine.Core.Ecs.Resources;

namespace MyGame.Resources;

public class WorldSizeResource : IResource
{
    public required float Left { get; init; }
    public required float Right { get; init; }
    public required float Top { get; init; }
    public required float Bottom { get; init; }
}
