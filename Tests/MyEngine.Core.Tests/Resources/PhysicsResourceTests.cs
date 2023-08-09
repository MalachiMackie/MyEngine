using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using System.Numerics;

namespace MyEngine.Core.Tests.Resources;

public class PhysicsResourceTests
{
    private readonly PhysicsResource _physicsResource = new();

    [Fact]
    public void Should_EnqueueAllCommandsInOrder()
    {
        var entityId = EntityId.Generate();
        var transform1 = new Transform();
        var transform2 = new Transform();
        _physicsResource.Update(1);
        _physicsResource.AddDynamicBody(entityId, transform1, 1);
        _physicsResource.AddStaticBody(entityId, transform2, 2);
        _physicsResource.AddDynamicBody2D(entityId, transform1, new BoxCollider2D(Vector2.One), 1);
        _physicsResource.AddStaticBody2D(entityId, transform2, new BoxCollider2D(Vector2.One), 1);
        _physicsResource.ApplyAngularImpulse(entityId, Vector3.One);
        _physicsResource.ApplyImpulse(entityId, Vector3.One);
        _physicsResource.RemoveDynamicBody(entityId);
        _physicsResource.RemoveStaticBody(entityId);
        _physicsResource.UpdateDynamicTransform(entityId, transform1);

        _physicsResource.PhysicsCommands.Should().BeEquivalentTo(new PhysicsResource.IPhysicsCommand[]
        {
            new PhysicsResource.UpdateCommand(1),
            new PhysicsResource.AddDynamicBodyCommand(entityId, transform1, 1),
            new PhysicsResource.AddStaticBodyCommand(entityId, transform2, 2),
            new PhysicsResource.AddDynamicBody2DCommand(entityId, transform1, new BoxCollider2D(Vector2.One), 1),
            new PhysicsResource.AddStaticBody2DCommand(entityId, transform2, new BoxCollider2D(Vector2.One), 1),
            new PhysicsResource.ApplyAngularImpulseCommand(entityId, Vector3.One),
            new PhysicsResource.ApplyImpulseCommand(entityId, Vector3.One),
            new PhysicsResource.RemoveDynamicBodyCommand(entityId),
            new PhysicsResource.RemoveStaticBodyCommand(entityId),
            new PhysicsResource.UpdateDynamicTransformCommand(entityId, transform1),
        }, options => options.RespectingRuntimeTypes());
    }
}
