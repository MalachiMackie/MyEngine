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

        public void AddStaticBody(EntityId entityId, Transform transform)
        {
            PhysicsCommands.Enqueue(new AddStaticBodyCommand(entityId, transform));
        }

        public void RemoveDynamicBody(EntityId entityId)
        {
            PhysicsCommands.Enqueue(new RemoveDynamicBodyCommand(entityId));
        }

        public void AddDynamicBody(EntityId entityId, Transform transform)
        {
            PhysicsCommands.Enqueue(new AddDynamicBodyCommand(entityId, transform));
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

        internal interface IPhysicsCommand
        {
        }

        internal record UpdateCommand(double dt) : IPhysicsCommand;
        internal record AddDynamicBodyCommand(EntityId entityId, Transform transform) : IPhysicsCommand;
        internal record AddStaticBodyCommand(EntityId entityId, Transform transform) : IPhysicsCommand;
        internal record RemoveDynamicBodyCommand(EntityId entityId) : IPhysicsCommand;
        internal record RemoveStaticBodyCommand(EntityId entityId) : IPhysicsCommand;
        internal record UpdateStaticTransformCommand(EntityId entityId, Transform transform) : IPhysicsCommand;
        internal record UpdateDynamicTransformCommand(EntityId entityId, Transform transform) : IPhysicsCommand;
        internal record ApplyImpulseCommand(EntityId entityId, Vector3 impulse) : IPhysicsCommand;
    }
}
