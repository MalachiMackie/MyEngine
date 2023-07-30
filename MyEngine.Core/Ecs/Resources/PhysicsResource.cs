using MyEngine.Core;
using MyEngine.Core.Ecs;
using System.Numerics;

namespace MyEngine.Core.Ecs.Resources
{
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

        public void AddStaticBody(EntityId entityId, Transform transform, float bounciness)
        {
            PhysicsCommands.Enqueue(new AddStaticBodyCommand(entityId, transform, bounciness));
        }

        public void AddStaticBody2D(EntityId entityId, Transform transform, float bounciness)
        {
            PhysicsCommands.Enqueue(new AddStaticBody2DCommand(entityId, transform, bounciness));
        }

        public void RemoveDynamicBody(EntityId entityId)
        {
            PhysicsCommands.Enqueue(new RemoveDynamicBodyCommand(entityId));
        }

        public void AddDynamicBody(EntityId entityId, Transform transform, float bounciness)
        {
            PhysicsCommands.Enqueue(new AddDynamicBodyCommand(entityId, transform, bounciness));
        }

        public void AddDynamicBody2D(EntityId entityId, Transform transform, float bounciness)
        {
            PhysicsCommands.Enqueue(new AddDynamicBody2DCommand(entityId, transform, bounciness));
        }

        public void UpdateStaticTransform(EntityId entityId, Transform transform)
        {
            PhysicsCommands.Enqueue(new UpdateStaticTransformCommand(entityId, transform));
        }

        public void UpdateDynamicTransform(EntityId entityId, Transform transform)
        {
            PhysicsCommands.Enqueue(new UpdateDynamicTransformCommand(entityId, transform));
        }

        public void ApplyImpulse(EntityId entityId, Vector3 impulse)
        {
            PhysicsCommands.Enqueue(new ApplyImpulseCommand(entityId, impulse));
        }

        public void ApplyAngularImpulse(EntityId entityId, Vector3 impulse)
        {
            PhysicsCommands.Enqueue(new ApplyAngularImpulseCommand(entityId, impulse));
        }

        internal interface IPhysicsCommand
        {
        }

        internal record UpdateCommand(double dt) : IPhysicsCommand;
        internal record AddDynamicBodyCommand(EntityId entityId, Transform transform, float bounciness) : IPhysicsCommand;
        internal record AddDynamicBody2DCommand(EntityId entityId, Transform transform, float bounciness) : IPhysicsCommand;
        internal record AddStaticBodyCommand(EntityId entityId, Transform transform, float bounciness) : IPhysicsCommand;
        internal record AddStaticBody2DCommand(EntityId entityId, Transform transform, float bounciness) : IPhysicsCommand;
        internal record RemoveDynamicBodyCommand(EntityId entityId) : IPhysicsCommand;
        internal record RemoveStaticBodyCommand(EntityId entityId) : IPhysicsCommand;
        internal record UpdateStaticTransformCommand(EntityId entityId, Transform transform) : IPhysicsCommand;
        internal record UpdateDynamicTransformCommand(EntityId entityId, Transform transform) : IPhysicsCommand;
        internal record ApplyImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
        internal record ApplyAngularImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
    }
}
