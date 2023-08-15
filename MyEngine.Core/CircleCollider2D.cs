namespace MyEngine.Core;

public class CircleCollider2D : ICollider2D
{
    public float Radius { get; set; }

    public CircleCollider2D(float radius)
    {
        Radius = radius;
    }
}
