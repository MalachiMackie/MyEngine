using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Physics;

public interface IPhysicsAdapter : IResource
{
    IEnumerable<ColliderPosition> GetAllColliderPositions();

    IEnumerable<EntityId> GetStaticBodies();

    IEnumerable<EntityId> GetDynamicBodies();
    void ApplyImpulse(EntityId entityId, Vector3 impulse);
    void ApplyAngularImpulse(EntityId entityId, Vector3 impulse);
    (Vector3 Position, Quaternion Rotation, Vector3 Velocity) GetDynamicPhysicsInfo(EntityId entityId);
    Result<Unit> ApplyDynamicPhysicsTransform(EntityId entityId, GlobalTransform transform);
    Result<Unit> ApplyStaticPhysicsTransform(EntityId entityId, GlobalTransform transform);
    Result<Unit> AddDynamicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider);
    Result<Unit> AddKinematicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider);
    Result<Unit> AddDynamicBody(EntityId entityId, GlobalTransform transform);
    void RemoveDynamicBody(EntityId entityId);
    Result<Unit> AddStaticBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider2D);
    void SetDynamicBody2DVelocity(EntityId entityId, Vector2 velocity);
    Result<Unit> AddStaticBody(EntityId entityId, GlobalTransform transform);
    void RemoveStaticBody(EntityId entityId);
    void Update(double dt, out IEnumerable<Collision> newCollisions, out IEnumerable<Collision> continuingCollisions, out IEnumerable<Collision> oldCollisions);


    readonly record struct ColliderPositionCollider(BoxCollider2D? BoxCollider2D, CircleCollider2D? CircleCollider2D);
    readonly record struct ColliderPosition(
        Vector3 Position,
        Quaternion Rotation,
        ColliderPositionCollider Collider,
        RigidBodyType RigidBodyType);
    enum RigidBodyType
    {
        Static,
        Dynamic,
        Kinematic
    }
}
