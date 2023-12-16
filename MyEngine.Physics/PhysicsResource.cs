using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Physics;

public class PhysicsResource : IResource
{
    internal readonly Queue<IPhysicsCommand> PhysicsCommands = new();

    public PhysicsResource()
    {
    }

    internal void Update(double dt)
    {
        PhysicsCommands.Enqueue(new UpdateCommand(dt));
    }

    public void RemoveStaticBody(EntityId entityId)
    {
        PhysicsCommands.Enqueue(new RemoveStaticBodyCommand(entityId));
    }

    public void AddStaticBody(EntityId entityId, GlobalTransform transform)
    {
        PhysicsCommands.Enqueue(new AddStaticBodyCommand(entityId, transform));
    }

    public void AddStaticBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        PhysicsCommands.Enqueue(new AddStaticBody2DCommand(entityId, transform, collider));
    }

    public void RemoveDynamicBody(EntityId entityId)
    {
        PhysicsCommands.Enqueue(new RemoveDynamicBodyCommand(entityId));
    }

    public void AddDynamicBody(EntityId entityId, GlobalTransform transform)
    {
        PhysicsCommands.Enqueue(new AddDynamicBodyCommand(entityId, transform));
    }

    public void AddDynamicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        PhysicsCommands.Enqueue(new AddDynamicBody2DCommand(entityId, transform, collider));
    }

    public void AddKinematicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        PhysicsCommands.Enqueue(new AddKinematicBody2DCommand(entityId, transform, collider));
    }

    internal void SetDynamicTransform(EntityId entityId, GlobalTransform transform)
    {
        PhysicsCommands.Enqueue(new SetDynamicTransformCommand(entityId, transform));
    }

    internal void SetStaticTransform(EntityId entityId, GlobalTransform transform)
    {
        PhysicsCommands.Enqueue(new SetStaticTransformCommand(entityId, transform));
    }

    internal void PhysicsWriteBack(EntityId entityId, TransformComponent transform, GlobalTransform? parentTransform, VelocityComponent? velocity)
    {
        PhysicsCommands.Enqueue(new PhysicsWriteBackCommand(entityId, transform, parentTransform, velocity));
    }

    public void ApplyImpulse(EntityId entityId, Vector3 impulse)
    {
        PhysicsCommands.Enqueue(new ApplyImpulseCommand(entityId, impulse));
    }

    public void ApplyAngularImpulse(EntityId entityId, Vector3 impulse)
    {
        PhysicsCommands.Enqueue(new ApplyAngularImpulseCommand(entityId, impulse));
    }

    public void SetBody2DVelocity(EntityId entityId, Vector2 velocity)
    {
        PhysicsCommands.Enqueue(new SetBody2DVelocityCommand(entityId, velocity));
    }

    internal interface IPhysicsCommand
    {
    }

    internal record UpdateCommand(double dt) : IPhysicsCommand;
    internal record AddDynamicBodyCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record AddDynamicBody2DCommand(EntityId entityId, GlobalTransform transform, ICollider2D collider) : IPhysicsCommand;
    internal record AddKinematicBody2DCommand(EntityId entityId, GlobalTransform transform, ICollider2D collider) : IPhysicsCommand;
    internal record AddStaticBodyCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record AddStaticBody2DCommand(EntityId entityId, GlobalTransform transform, ICollider2D collider) : IPhysicsCommand;
    internal record RemoveDynamicBodyCommand(EntityId entityId) : IPhysicsCommand;
    internal record RemoveStaticBodyCommand(EntityId entityId) : IPhysicsCommand;
    internal record SetDynamicTransformCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record SetStaticTransformCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record SetBody2DVelocityCommand(EntityId entityId, Vector2 velocity) : IPhysicsCommand;
    internal record PhysicsWriteBackCommand(EntityId entityId, TransformComponent transform, GlobalTransform? parentTransform, VelocityComponent? velocity) : IPhysicsCommand;
    internal record ApplyImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
    internal record ApplyAngularImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
}
