using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;
using System.Diagnostics;
using System.Numerics;

namespace MyEngine.Physics;

internal struct MyPoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

    public bool AllowSubstepsForUnconstrainedBodies => false;

    public bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation)
    {
    }


    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
    }

    public void PrepareForIntegration(float dt)
    {
    }
}

public struct Impact
{
    public BodyHandle? bodyHandleA;
    public StaticHandle? staticHandleA;
    public BodyHandle? bodyHandleB;
    public StaticHandle? staticHandleB;

    public Vector3 normal;

    public override bool Equals(object? obj)
    {
        return obj is Impact impact &&
               EqualityComparer<BodyHandle?>.Default.Equals(bodyHandleA, impact.bodyHandleA) &&
               EqualityComparer<StaticHandle?>.Default.Equals(staticHandleA, impact.staticHandleA) &&
               EqualityComparer<BodyHandle?>.Default.Equals(bodyHandleB, impact.bodyHandleB) &&
               EqualityComparer<StaticHandle?>.Default.Equals(staticHandleB, impact.staticHandleB);
    }

    public override int GetHashCode()
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

internal struct MyNarrowPhaseCallback : INarrowPhaseCallbacks
{
    public CollidableProperty<SimpleMaterial> CollidableMaterials;

    public MyNarrowPhaseCallback()
    {
        Impacts = new();
        CollidableMaterials = new();
    }

    public List<Impact> Impacts;

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        return a.Mobility != CollidableMobility.Static || b.Mobility != CollidableMobility.Static;
    }

    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
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

        Impacts.Add(new Impact
        {
            bodyHandleA = aIsBody ? pair.A.BodyHandle : null,
            staticHandleA = aIsBody ? null : pair.A.StaticHandle,
            bodyHandleB = bIsBody ? pair.B.BodyHandle : null,
            staticHandleB = bIsBody ? null : pair.B.StaticHandle,
            normal = normal
        });

        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
    }

    public void Initialize(Simulation simulation)
    {
        CollidableMaterials.Initialize(simulation);
    }
}

public class MyPhysics : IResource
{
    private readonly Simulation _simulation;
    private readonly BufferPool _bufferPool;

    // todo: record
    private readonly Dictionary<EntityId, (StaticHandle Handle, TypedIndex ShapeIndex, ShapeType ShapeType)> _staticHandles = new();
    private readonly Dictionary<EntityId, (BodyHandle Handle, TypedIndex ShapeIndex, ShapeType ShapeType)> _dynamicHandles = new();

    private NarrowPhase<MyNarrowPhaseCallback> NarrowPhase
    {
        get
        {
            if (_simulation.NarrowPhase is not NarrowPhase<MyNarrowPhaseCallback> narrowPhase)
            {
                throw new UnreachableException();
            }

            return narrowPhase;
        }
    }

    public MyPhysics()
    {
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(_bufferPool,
            new MyNarrowPhaseCallback(),
            new MyPoseIntegratorCallbacks(),
            new SolveDescription(6, 1));
    }

    public void Update(double dt, out IEnumerable<Collision> newCollisions, out IEnumerable<Collision> continuingCollisions, out IEnumerable<Collision> oldCollisions)
    {
        if (_simulation.NarrowPhase is not NarrowPhase<MyNarrowPhaseCallback> narrowPhase)
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

        newCollisions = newImpacts.Select(ImpactToCollision).Where(x => x is not null).Cast<Collision>();
        continuingCollisions = continuingImpacts.Select(ImpactToCollision).Where(x => x is not null).Cast<Collision>();
        oldCollisions = oldImpacts.Select(ImpactToCollision).Where(x => x is not null).Cast<Collision>();
    }

    private Collision? ImpactToCollision(Impact impact)
    {
        EntityId entityIdA;
        EntityId entityIdB;
        if (impact.bodyHandleA.HasValue)
        {
            (entityIdA, var _) = _dynamicHandles.FirstOrDefault(y => y.Value.Handle == impact.bodyHandleA.Value);
        } else if (impact.staticHandleA.HasValue)
        {
            (entityIdA, var _) = _staticHandles.FirstOrDefault(y => y.Value.Handle == impact.staticHandleA.Value);
        } else
        {
            return null;
        }

        if (impact.bodyHandleB.HasValue)
        {
            (entityIdB, var _) = _dynamicHandles.FirstOrDefault(y => y.Value.Handle == impact.bodyHandleB.Value);
        } else if (impact.staticHandleB.HasValue)
        {
            (entityIdB, var _) = _staticHandles.FirstOrDefault(y => y.Value.Handle == impact.staticHandleB.Value);
        } else
        {
            return null;
        }

        return new Collision
        {
            EntityA = entityIdA,
            EntityB = entityIdB,
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

    public enum RigidBodyType
    {
        Static,
        Dynamic,
        Kinematic
    }

    private enum ShapeType
    {
        Box2D,
        Circle2D,
        Box3D,
        Sphere3D
    }

    public readonly record struct ColliderPosition(
        Vector3 Position,
        Quaternion Rotation,
        OneOf<BoxCollider2D, CircleCollider2D> Collider,
        RigidBodyType RigidBodyType
        );

    private OneOf<BoxCollider2D, CircleCollider2D> GetColliderShape(TypedIndex shapeIndex, ShapeType shapeType)
    {
        switch (shapeType)
        {
            case ShapeType.Box3D:
            case ShapeType.Box2D:
                {
                    var shape = _simulation.Shapes.GetShape<Box>(shapeIndex.Index);
                    return new OneOf<BoxCollider2D, CircleCollider2D>(new BoxCollider2D(new Vector2(shape.Width, shape.Height)));
                }
            case ShapeType.Circle2D:
            case ShapeType.Sphere3D:
                {
                    var shape = _simulation.Shapes.GetShape<Sphere>(shapeIndex.Index);
                    return new OneOf<BoxCollider2D, CircleCollider2D>(new CircleCollider2D(shape.Radius));
                }
            default:
                {
                    throw new UnreachableException();
                }
        }
    }

    // todo: add system to debug render these
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

    public readonly record struct AddStaticBodyError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, AddStaticBodyError> AddStaticBody(EntityId entityId, GlobalTransform transform)
    {
        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new AddStaticBodyError(err));
        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, AddStaticBodyError>(positionRotationScaleResult.UnwrapError());
        }

        var (position, rotation, _) = positionRotationScale;

        // todo: Collider3D
        var shape = _simulation.Shapes.Add(new Box(transform.Scale.X, transform.Scale.Y, transform.Scale.Z));
        var shapeType = ShapeType.Box3D;
        var handle = _simulation.Statics.Add(new StaticDescription(position, rotation, shape));

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _staticHandles[entityId] = (handle, shape, shapeType);

        return Result.Success<Unit, AddStaticBodyError>(Unit.Value);
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

    public readonly record struct AddStaticBody2DError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, AddStaticBody2DError> AddStaticBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider2D)
    {
        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new AddStaticBody2DError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, AddStaticBody2DError>(positionRotationScaleResult.UnwrapError());
        }
        var (position, rotation, _) = positionRotationScale;

        // todo: don't require mass
        var (shape, _, shapeType) = AddColliderAsShape(collider2D, transform.Scale, 10f);
        var handle = _simulation.Statics.Add(new StaticDescription(position, rotation, shape));

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;


        _staticHandles[entityId] = (handle, shape, shapeType);

        return Result.Success<Unit, AddStaticBody2DError>(Unit.Value);
    }

    public void RemoveDynamicBody(EntityId entityId)
    {
        var (handle, shape, _) = _dynamicHandles[entityId];
        _dynamicHandles.Remove(entityId);
        _simulation.Bodies.Remove(handle);
        _simulation.Shapes.Remove(shape);
    }

    public readonly record struct AddDynamicBodyError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, AddDynamicBodyError> AddDynamicBody(EntityId entityId, GlobalTransform transform, float bounciness)
    {
        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new AddDynamicBodyError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, AddDynamicBodyError>(positionRotationScaleResult.UnwrapError());
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
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = float.MaxValue,
            SpringSettings = new SpringSettings(5f + 25f * (1f - bounciness), 1f - bounciness)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _dynamicHandles.Add(entityId, (handle, shapeIndex, shapeType));

        return Result.Success<Unit, AddDynamicBodyError>(Unit.Value);
    }

    public readonly record struct AddKinematicbody2DError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, AddKinematicbody2DError> AddKinematicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        var (shapeIndex, _, shapeType) = AddColliderAsShape(collider, transform.Scale, 10f);

        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new AddKinematicbody2DError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, AddKinematicbody2DError>(positionRotationScaleResult.UnwrapError());
        }

        var (position, rotation, _) = positionRotationScale;

        var body = BodyDescription.CreateKinematic(
            new RigidPose(position, rotation),
            new BodyVelocity(),
            new CollidableDescription(shapeIndex),
            new BodyActivityDescription(0.01f));

        var handle = _simulation.Bodies.Add(body);

        _dynamicHandles.Add(entityId, (handle, shapeIndex, shapeType));

        return Result.Success<Unit, AddKinematicbody2DError>(Unit.Value);
    }

    public readonly record struct AddDynamicBody2DError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, AddDynamicBody2DError> AddDynamicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider, float bounciness)
    {
        var (shapeIndex, inertia, shapeType) = AddColliderAsShape(collider, transform.Scale, 10f);
        var inverseInertiaTensor = inertia.InverseInertiaTensor;

        // dont allow rotation along X or Y Axis for 2D
        inverseInertiaTensor.XX = 0f;
        inverseInertiaTensor.YY = 0f;

        inertia.InverseInertiaTensor = inverseInertiaTensor;

        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new AddDynamicBody2DError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, AddDynamicBody2DError>(positionRotationScaleResult.UnwrapError());
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
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = float.MaxValue,
            // full bounce target: 5f, 1f
            // zero bounce target: 30f, 0f
            // todo: full and zero bounce work well, half bounciness doesnt do half bounce
            SpringSettings = new SpringSettings(5f + 25f * (1f - bounciness), 1f - bounciness)
        };

        NarrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _dynamicHandles.Add(entityId, (handle, shapeIndex, shapeType));

        return Result.Success<Unit, AddDynamicBody2DError>(Unit.Value);
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

    public (Vector3 Position, Quaternion Rotation) GetDynamicPhysicsTransform(EntityId entityId)
    {
        var (handle, _, _) = _dynamicHandles[entityId];
        var body = _simulation.Bodies[handle];
        var pose = body.Pose;

        return (pose.Position, pose.Orientation);
    }

    public readonly record struct ApplyDynamicPhysicsTransformError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, ApplyDynamicPhysicsTransformError> ApplyDynamicPhysicsTransform(EntityId entityId, GlobalTransform transform)
    {
        var (handle, _, _) = _dynamicHandles[entityId];
        var body = _simulation.Bodies[handle];

        body.GetDescription(out var description);

        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new ApplyDynamicPhysicsTransformError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, ApplyDynamicPhysicsTransformError>(positionRotationScaleResult.UnwrapError());
        }

        var (position, rotation, _) = positionRotationScale;


        description.Pose.Position = position;
        description.Pose.Orientation = rotation;
        body.ApplyDescription(description);

        return Result.Success<Unit, ApplyDynamicPhysicsTransformError>(Unit.Value);
    }

    public readonly record struct ApplyStaticPhysicsTransformError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, ApplyStaticPhysicsTransformError> ApplyStaticPhysicsTransform(EntityId entityId, GlobalTransform transform)
    {
        var (handle, _, _) = _staticHandles[entityId];
        var body = _simulation.Statics[handle];

        var positionRotationScaleResult = transform.GetPositionRotationScale()
            .MapError(err => new ApplyStaticPhysicsTransformError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, ApplyStaticPhysicsTransformError>(positionRotationScaleResult.UnwrapError());
        }

        var (position, rotation, _) = positionRotationScale;

        body.GetDescription(out var description);

        description.Pose.Position = position;
        description.Pose.Orientation = rotation;
        body.ApplyDescription(description);

        return Result.Success<Unit, ApplyStaticPhysicsTransformError>(Unit.Value);
    }
}
