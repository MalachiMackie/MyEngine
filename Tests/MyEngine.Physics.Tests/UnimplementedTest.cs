namespace MyEngine.Physics.Tests;

public class UnimplementedTest
{
    [Fact]
    public void Test1()
    {
        new Collider2DComponent(new BoxCollider2D(new System.Numerics.Vector2()));
    }
}