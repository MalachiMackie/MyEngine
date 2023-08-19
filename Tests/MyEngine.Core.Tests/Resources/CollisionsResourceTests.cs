using System.Numerics;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Core.Tests.Resources;

public class CollisionsResourceTests
{
    private readonly CollisionsResource _collisionsResource = new();

    [Fact]
    public void NewCollisions_Should_ContainNewCollisions()
    {
        var collision = new Collision()
        {
            EntityA = EntityId.Generate(),
            EntityB = EntityId.Generate(),
            Normal = Vector3.Zero
        };
        _collisionsResource._newCollisions.Add(collision);
        _collisionsResource._newCollisions.Add(collision);

        _collisionsResource.NewCollisions.Should().BeEquivalentTo(new[] { collision, collision });
    }
}
