using System.Numerics;

namespace MyEngine.Physics;

public class BoxCollider2D : ICollider2D
{
    public Vector2 Dimensions { get; }

    public BoxCollider2D(Vector2 dimensions)
    {
        Dimensions = dimensions;
    }
}
