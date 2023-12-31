﻿using System.Diagnostics;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Utils;

using static MyEngine.Physics.IPhysicsAdapter;

namespace MyEngine.Physics;

internal struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

    public readonly bool AllowSubstepsForUnconstrainedBodies => false;

    public readonly bool IntegrateVelocityForKinematics => false;

    public readonly void Initialize(Simulation simulation)
    {
    }

    public readonly void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
    }

    public readonly void PrepareForIntegration(float dt)
    {
    }
}

public struct Impact
{
    public BodyHandle? bodyHandleA;
    public BodyVelocity? bodyVelocityA;
    public StaticHandle? staticHandleA;
    public BodyHandle? bodyHandleB;
    public BodyVelocity? bodyVelocityB;
    public StaticHandle? staticHandleB;

    public Vector3 normal;

    public readonly override bool Equals(object? obj)
    {
        return obj is Impact impact &&
               EqualityComparer<BodyHandle?>.Default.Equals(bodyHandleA, impact.bodyHandleA) &&
               EqualityComparer<StaticHandle?>.Default.Equals(staticHandleA, impact.staticHandleA) &&
               EqualityComparer<BodyHandle?>.Default.Equals(bodyHandleB, impact.bodyHandleB) &&
               EqualityComparer<StaticHandle?>.Default.Equals(staticHandleB, impact.staticHandleB);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(bodyHandleA, staticHandleA, bodyHandleB, staticHandleB);
    }

    public static bool operator ==(Impact left, Impact right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Impact left, Impact right)
    {
        return !(left == right);
    }
}

internal struct SimpleMaterial
{
    public SpringSettings SpringSettings;
    public float FrictionCoefficient;
    public float MaximumRecoveryVelocity;
}

internal struct NarrowPhaseCallback : INarrowPhaseCallbacks
{
    public CollidableProperty<SimpleMaterial> CollidableMaterials;
    private Simulation _simulation = null!;

    public NarrowPhaseCallback()
    {
        Impacts = new();
        CollidableMaterials = new();
    }

    public List<Impact> Impacts;

    public readonly bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        return a.Mobility != CollidableMobility.Static || b.Mobility != CollidableMobility.Static;
    }

    public readonly bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        var a = CollidableMaterials[pair.A];
        var b = CollidableMaterials[pair.B];
        pairMaterial.FrictionCoefficient = a.FrictionCoefficient * b.FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MathF.Max(a.MaximumRecoveryVelocity, b.MaximumRecoveryVelocity);
        pairMaterial.SpringSettings = pairMaterial.MaximumRecoveryVelocity == a.MaximumRecoveryVelocity ? a.SpringSettings : b.SpringSettings;

        var aIsBody = pair.A.Mobility != CollidableMobility.Static;
        var bIsBody = pair.B.Mobility != CollidableMobility.Static;

        if (manifold.Count == 0)
        {
            return false;
        }

        var normal = manifold.GetNormal(ref manifold, contactIndex: 0); // todo: handle multiple collisions

        BodyDescription bodyA = new BodyDescription();
        BodyDescription bodyB = new BodyDescription();

        if (aIsBody)
        {
            _simulation.Bodies.GetDescription(pair.A.BodyHandle, out bodyA);
        }
        if (bIsBody)
        {
            _simulation.Bodies.GetDescription(pair.B.BodyHandle, out bodyB);
        }


        Impacts.Add(new Impact
        {
            bodyHandleA = aIsBody ? pair.A.BodyHandle : null,
            bodyVelocityA = aIsBody ? bodyA.Velocity : null,
            staticHandleA = aIsBody ? null : pair.A.StaticHandle,
            bodyHandleB = bIsBody ? pair.B.BodyHandle : null,
            bodyVelocityB = bIsBody ? bodyB.Velocity : null,
            staticHandleB = bIsBody ? null : pair.B.StaticHandle,
            normal = normal
        });

        return true;
    }

    public readonly bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public readonly void Dispose()
    {
    }

    public void Initialize(Simulation simulation)
    {
        CollidableMaterials.Initialize(simulation);
        _simulation = simulation;
    }
}

public class BepuPhysicsAdapter : IPhysicsAdapter
{
    private readonly Simulation _simulation;
    private readonly BufferPool _bufferPool;

    // todo: record
    private readonly Dictionary<EntityId, (StaticHandle Handle, TypedIndex ShapeIndex, ShapeType ShapeType)> _staticHandles = new();
    private readonly Dictionary<EntityId, (BodyHandle Handle, TypedIndex ShapeIndex, ShapeType ShapeType)> _dynamicHandles = new();

    private NarrowPhase<NarrowPhaseCallback> NarrowPhase
    {
        get
        {
            if (_simulation.NarrowPhase is not NarrowPhase<NarrowPhaseCallback> narrowPhase)
            {
                throw new UnreachableException();
            }

            return narrowPhase;
        }
    }

    public BepuPhysicsAdapter()
    {
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(_bufferPool,
            new NarrowPhaseCallback(),
            new PoseIntegratorCallbacks(),
            new SolveDescription(6, 4));
    }

    public void Update(double dt, out IEnumerable<Collision> newCollisions, out IEnumerable<Collision> continuingCollisions, out IEnumerable<Collision> oldCollisions)
    {
        if (_simulation.NarrowPhase is not NarrowPhase<NarrowPhaseCallback> narrowPhase)
        {
            newCollisions = null!;
            continuingCollisions = null!;
            oldCollisions = null!;
            return;
        }

        var impacts = narrowPhase.Callbacks.Impacts;

        var existingImpacts = impacts.ToArray();

        impacts.Clear();
        _simulation.Timestep((float)dt);

        var newImpacts = impacts.Except(existingImpacts);
        var oldImpacts = existingImpacts.Except(impacts);
        var continuingImpacts = existingImpacts.Except(oldImpacts);

        newCollisions = newImpacts.Select(ImpactToCollision).WhereNotNull();
        continuingCollisions = continuingImpacts.Select(ImpactToCollision).WhereNotNull();
        oldCollisions = oldImpacts.Select(ImpactToCollision).WhereNotNull();
    }

    private Collision? ImpactToCollision(Impact impact)
    {
        EntityId entityIdA;
        EntityId entityIdB;
        if (impact.bodyHandleA.HasValue)
        {
            (entityIdA, var _) = _dynamicHandles.FirstOrDefault(y => y.Value.Handle == impact.bodyHandleA.Value);
        }
        else if (impact.staticHandleA.HasValue)
        {
            (entityIdA, var _) = _staticHandles.FirstOrDefault(y => y.Value.Handle == impact.staticHandleA.Value);
        }
        else
        {
            return null;
        }

        if (impact.bodyHandleB.HasValue)
        {
            (entityIdB, var _) = _dynamicHandles.FirstOrDefault(y => y.Value.Handle == impact.bodyHandleB.Value);
        }
        else if (impact.staticHandleB.HasValue)
        {
            (entityIdB, var _) = _staticHandles.FirstOrDefault(y => y.Value.Handle == impact.staticHandleB.Value);
        }
        else
        {
            return null;
        }

        return new Collision
        {
            EntityA = entityIdA,
            EntityAVelocity = impact.bodyVelocityA?.Linear,
            EntityB = entityIdB,
            EntityBVelocity = impact.bodyVelocityB?.Linear,
            Normal = impact.normal
        };
    }

    public IEnumerable<EntityId> GetStaticBodies()
    {
        return _staticHandles.Keys;
    }

    public IEnumerable<EntityId> GetDynamicBodies()
    {
        return _dynamicHandles.Keys;
    }

    private enum ShapeType
    {
        Box2D,
        Circle2D,
        Box3D,
        Sphere3D
    }

    private ColliderPositionCollider GetColliderShape(TypedIndex shapeIndex, ShapeType shapeType)
    {
        switch (shapeType)
        {
            case ShapeType.Box3D:
            case ShapeType.Box2D:
                {
                    var shape = _simulation.Shapes.GetShape<Box>(shapeIndex.Index);
                    return new ColliderPositionCollider(new BoxCollider2D(new Vector2(shape.Width, shape.Height)), null);
                }
            case ShapeType.Circle2D:
            case ShapeType.Sphere3D:
                {
                    var shape = _simulation.Shapes.GetShape<Sphere>(shapeIndex.Index);
                    return new ColliderPositionCollider(null, new CircleCollider2D(shape.Radius));
                }
            default:
                {
                    throw new UnreachableException();
                }
        }
    }

    public IEnumerable<ColliderPosition> GetAllColliderPositions()
    {
        foreach (var (_, (staticHandle, shapeIndex, shapeType)) in _staticHandles)
        {
            var staticBody = _simulation.Statics[staticHandle];
            staticBody.GetDescription(out var description);

            var collider = GetColliderShape(shapeIndex, shapeType);
            yield return new ColliderPosition(description.Pose.Position, description.Pose.Orientation, collider, RigidBodyType.Static);
        }

        foreach (var (_, (dynamicHandle, shapeIndex, shapeType)) in _dynamicHandles)
        {
            var body = _simulation.Bodies[dynamicHandle];
            body.GetDescription(out var description);

            var collider = GetColliderShape(shapeIndex, shapeType);
            yield return new ColliderPosition(description.Pose.Position, description.Pose.Orientation, collider, body.Kinematic ? RigidBodyType.Kinematic : RigidBodyType.Dynamic);
        }
    }

    public void RemoveStaticBody(EntityId entityId)
    {
        if (!_staticHandles.TryGetValue(entityId, out var handles))
        {
            return;
        }
        var (handle, shape, _) = handles;
        _staticHandles.Remove(entityId);
        _simulation.Statics.Remove(handle);
        _simulation.Shapes.Remove(shape);
    }

    public Result<Unit> AddStaticBody(EntityId entityId, GlobalTransform transform)
    {
        var positionRotationScaleResult = transform.GetPositionRotationScale();
        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }

        var (position, rotation, _) = positionRotationScale;

        // todo: Collider3D
        var shape = _simulation.Shapes.Add(new Box(transform.Scale.X, transform.Scale.Y, transform.Scale.Z));
        var shapeType = ShapeType.Box3D;
        var handle = _simulation.Statics.Add(new StaticDescription(position, rotation, shape));

        var material = new SimpleMaterial
        {
            // todo: FrictionComponent
            FrictionCoefficient = 0f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _staticHandles[entityId] = (handle, shape, shapeType);

        return Result.Success<Unit>(Unit.Value);
    }

    private (TypedIndex ShapeIndex, BodyInertia ShapeInertia, ShapeType ShapeType) AddColliderAsShape(ICollider2D collider2D, Vector3 scale, float mass)
    {
        switch (collider2D)
        {
            case BoxCollider2D boxCollider:
                {
                    var shape = new Box(boxCollider.Dimensions.X * scale.X, boxCollider.Dimensions.Y * scale.Y, 1000f);
                    var inertia = shape.ComputeInertia(mass);

                    var shapeIndex = _simulation.Shapes.Add(shape);

                    return (shapeIndex, inertia, ShapeType.Box2D);
                }
            case CircleCollider2D circleCollider:
                {
                    var shape = new Sphere(circleCollider.Radius);
                    var inertia = shape.ComputeInertia(mass);

                    var shapeIndex = _simulation.Shapes.Add(shape);

                    return (shapeIndex, inertia, ShapeType.Circle2D);
                }
            default:
                throw new NotImplementedException();
        }
    }

    public void SetDynamicBody2DVelocity(EntityId entityId, Vector2 velocity)
    {
        var (bodyHandle, _, _) = _dynamicHandles[entityId];
        var bodyRef = _simulation.Bodies[bodyHandle];
        ref var currentVelocity = ref bodyRef.Velocity;
        currentVelocity.Linear = velocity.Extend(currentVelocity.Linear.Z);
    }

    public Result<Unit> AddStaticBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider2D)
    {
        var positionRotationScaleResult = transform.GetPositionRotationScale();

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }
        var (position, rotation, _) = positionRotationScale;

        // todo: don't require mass
        var (shape, _, shapeType) = AddColliderAsShape(collider2D, transform.Scale, 10f);
        var handle = _simulation.Statics.Add(new StaticDescription(position, rotation, shape));

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 0f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;


        _staticHandles[entityId] = (handle, shape, shapeType);

        return Result.Success<Unit>(Unit.Value);
    }

    public void RemoveDynamicBody(EntityId entityId)
    {
        var (handle, shape, _) = _dynamicHandles[entityId];
        _dynamicHandles.Remove(entityId);
        _simulation.Bodies.Remove(handle);
        _simulation.Shapes.Remove(shape);
    }

    public Result<Unit> AddDynamicBody(EntityId entityId, GlobalTransform transform)
    {
        var positionRotationScaleResult = transform.GetPositionRotationScale();

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }

        var (position, rotation, _) = positionRotationScale;

        var shape = new Box(transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
        var shapeType = ShapeType.Box3D;
        var shapeIndex = _simulation.Shapes.Add(shape);
        var handle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
            new RigidPose(position, rotation),
            new BodyVelocity(new Vector3(0f, 0f, 0f)),
            shape.ComputeInertia(10f),
            new CollidableDescription(shapeIndex),
            new BodyActivityDescription(0.01f)));

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 0f,
            MaximumRecoveryVelocity = float.MaxValue,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _dynamicHandles.Add(entityId, (handle, shapeIndex, shapeType));

        return Result.Success<Unit>(Unit.Value);
    }

    public Result<Unit> AddKinematicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        var (shapeIndex, _, shapeType) = AddColliderAsShape(collider, transform.Scale, 10f);

        var positionRotationScaleResult = transform.GetPositionRotationScale();

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }

        var (position, rotation, _) = positionRotationScale;

        var body = BodyDescription.CreateKinematic(
            new RigidPose(position, rotation),
            new BodyVelocity(),
            new CollidableDescription(shapeIndex),
            new BodyActivityDescription(0.01f));

        var handle = _simulation.Bodies.Add(body);

        _dynamicHandles.Add(entityId, (handle, shapeIndex, shapeType));

        return Result.Success<Unit>(Unit.Value);
    }

    public Result<Unit> AddDynamicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        var (shapeIndex, inertia, shapeType) = AddColliderAsShape(collider, transform.Scale, 10f);
        var inverseInertiaTensor = inertia.InverseInertiaTensor;

        // dont allow rotation along X or Y Axis for 2D
        inverseInertiaTensor.XX = 0f;
        inverseInertiaTensor.YY = 0f;

        inertia.InverseInertiaTensor = inverseInertiaTensor;

        var positionRotationScaleResult = transform.GetPositionRotationScale();

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }

        var (position, rotation, _) = positionRotationScale;

        var body = BodyDescription.CreateDynamic(
            new RigidPose(position, rotation),
            new BodyVelocity(),
            inertia,
            new CollidableDescription(shapeIndex),
            new BodyActivityDescription(0.01f));

        var handle = _simulation.Bodies.Add(body);

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 0f,
            MaximumRecoveryVelocity = float.MaxValue,
            // full bounce target: 5f, 1f
            // zero bounce target: 30f, 0f
            SpringSettings = new SpringSettings(30f, 1f)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _dynamicHandles.Add(entityId, (handle, shapeIndex, shapeType));

        return Result.Success<Unit>(Unit.Value);
    }

    public void ApplyImpulse(EntityId entityId, Vector3 impulse)
    {
        var (handle, _, _) = _dynamicHandles[entityId];
        var bodyReference = _simulation.Bodies[handle];
        bodyReference.Awake = true;
        bodyReference.ApplyLinearImpulse(impulse);
    }

    public void ApplyAngularImpulse(EntityId entityId, Vector3 impulse)
    {
        var (handle, _, _) = _dynamicHandles[entityId];
        var bodyReference = _simulation.Bodies[handle];
        bodyReference.Awake = true;
        bodyReference.ApplyAngularImpulse(impulse);
    }

    public (Vector3 Position, Quaternion Rotation, Vector3 Velocity) GetDynamicPhysicsInfo(EntityId entityId)
    {
        var (handle, _, _) = _dynamicHandles[entityId];
        var body = _simulation.Bodies[handle];
        var pose = body.Pose;

        return (pose.Position, pose.Orientation, body.Velocity.Linear);
    }

    public Result<Unit> ApplyDynamicPhysicsTransform(EntityId entityId, GlobalTransform transform)
    {
        var (handle, _, _) = _dynamicHandles[entityId];
        var body = _simulation.Bodies[handle];

        body.GetDescription(out var description);

        var positionRotationScaleResult = transform.GetPositionRotationScale();

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }

        var (position, rotation, _) = positionRotationScale;


        description.Pose.Position = position;
        description.Pose.Orientation = rotation;
        body.ApplyDescription(description);

        return Result.Success<Unit>(Unit.Value);
    }

    public Result<Unit> ApplyStaticPhysicsTransform(EntityId entityId, GlobalTransform transform)
    {
        var (handle, _, _) = _staticHandles[entityId];
        var body = _simulation.Statics[handle];

        var positionRotationScaleResult = transform.GetPositionRotationScale();

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, GlobalTransform.PositionRotationScale>(positionRotationScaleResult);
        }

        var (position, rotation, _) = positionRotationScale;

        body.GetDescription(out var description);

        description.Pose.Position = position;
        description.Pose.Orientation = rotation;
        body.ApplyDescription(description);

        return Result.Success<Unit>(Unit.Value);
    }
}
