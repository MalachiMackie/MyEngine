namespace MyEngine.Core.Ecs.Resources;

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

    public void AddDynamicBody(EntityId entityId, GlobalTransform transform, float bounciness)
    {
        PhysicsCommands.Enqueue(new AddDynamicBodyCommand(entityId, transform, bounciness));
    }

    public void AddDynamicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider, float bounciness)
    {
        PhysicsCommands.Enqueue(new AddDynamicBody2DCommand(entityId, transform, collider, bounciness));
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

    internal void UpdateTransformFromPhysics(EntityId entityId, Transform transform, GlobalTransform? parentTransform)
    {
        PhysicsCommands.Enqueue(new UpdateTransformFromPhysicsCommand(entityId, transform, parentTransform));
    }

    public void ApplyImpulse(EntityId entityId, Vector3 impulse)
    {
        PhysicsCommands.Enqueue(new ApplyImpulseCommand(entityId, impulse));
    }

    public void ApplyAngularImpulse(EntityId entityId, Vector3 impulse)
    {
        PhysicsCommands.Enqueue(new ApplyAngularImpulseCommand(entityId, impulse));
    }

    public void SetKinematicBody2DVelocity(EntityId entityId, Vector2 velocity)
    {
        PhysicsCommands.Enqueue(new SetKinematicBody2DVelocityCommand(entityId, velocity));
    }

    internal interface IPhysicsCommand
    {
    }

    internal record UpdateCommand(double dt) : IPhysicsCommand;
    internal record AddDynamicBodyCommand(EntityId entityId, GlobalTransform transform, float bounciness) : IPhysicsCommand;
    internal record AddDynamicBody2DCommand(EntityId entityId, GlobalTransform transform, ICollider2D collider, float bounciness) : IPhysicsCommand;
    internal record AddKinematicBody2DCommand(EntityId entityId, GlobalTransform transform, ICollider2D collider) : IPhysicsCommand;
    internal record AddStaticBodyCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record AddStaticBody2DCommand(EntityId entityId, GlobalTransform transform, ICollider2D collider) : IPhysicsCommand;
    internal record RemoveDynamicBodyCommand(EntityId entityId) : IPhysicsCommand;
    internal record RemoveStaticBodyCommand(EntityId entityId) : IPhysicsCommand;
    internal record SetDynamicTransformCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record SetStaticTransformCommand(EntityId entityId, GlobalTransform transform) : IPhysicsCommand;
    internal record SetKinematicBody2DVelocityCommand(EntityId entityId, Vector2 velocity) : IPhysicsCommand;
    internal record UpdateTransformFromPhysicsCommand(EntityId entityId, Transform transform, GlobalTransform? parentTransform) : IPhysicsCommand;
    internal record ApplyImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
    internal record ApplyAngularImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
}
