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

    private readonly Dictionary<EntityId, (StaticHandle Handle, TypedIndex ShapeIndex)> _staticHandles = new();
    private readonly Dictionary<EntityId, (BodyHandle Handle, TypedIndex ShapeIndex)> _dynamicHandles = new();

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

    public void RemoveStaticBody(EntityId entityId)
    {
        if (!_staticHandles.TryGetValue(entityId, out var handles))
        {
            return;
        }
        var (handle, shape) = handles;
        _staticHandles.Remove(entityId);
        _simulation.Statics.Remove(handle);
        _simulation.Shapes.Remove(shape);
    }

    public void AddStaticBody(EntityId entityId, GlobalTransform transform)
    {
        if (_simulation.NarrowPhase is not NarrowPhase<MyNarrowPhaseCallback> narrowPhase)
        {
            return;
        }

        var (position, rotation, _) = transform.GetPositionRotationScale();

        var shape = _simulation.Shapes.Add(new Box(transform.Scale.X, transform.Scale.Y, transform.Scale.Z));
        var handle = _simulation.Statics.Add(new StaticDescription(position, rotation, shape));

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        narrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _staticHandles[entityId] = (handle, shape);
    }

    private (TypedIndex ShapeIndex, BodyInertia ShapeInertia) AddColliderAsShape(ICollider2D collider2D, Vector3 scale, float mass)
    {
        switch (collider2D)
        {
            case BoxCollider2D boxCollider:
                {
                    var shape = new Box(boxCollider.Dimensions.X * scale.X, boxCollider.Dimensions.Y * scale.Y, 1000f);
                    var inertia = shape.ComputeInertia(mass);

                    var shapeIndex = _simulation.Shapes.Add(shape);

                    return (shapeIndex, inertia);
                }
            case CircleCollider2D circleCollider:
                {
                    // todo: how to scale this?
                    var shape = new Sphere(circleCollider.Radius * scale.X);
                    var inertia = shape.ComputeInertia(mass);

                    var shapeIndex = _simulation.Shapes.Add(shape);

                    return (shapeIndex, inertia);
                }
            default:
                throw new NotImplementedException();
        }
    }

    public void SetDynamicBody2DVelocity(EntityId entityId, Vector2 velocity)
    {
        var (bodyHandle, _) = _dynamicHandles[entityId];
        var bodyRef = _simulation.Bodies[bodyHandle];
        ref var currentVelocity = ref bodyRef.Velocity;
        currentVelocity.Linear = velocity.Extend(currentVelocity.Linear.Z);
    }

    public void AddStaticBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider2D)
    {
        if (_simulation.NarrowPhase is not NarrowPhase<MyNarrowPhaseCallback> narrowPhase)
        {
            return;
        }

        var (position, rotation, _) = transform.GetPositionRotationScale();

        // todo: don't require mass
        var (shape, _) = AddColliderAsShape(collider2D, transform.Scale, 10f);
        var handle = _simulation.Statics.Add(new StaticDescription(position, rotation, shape));

        var material = new SimpleMaterial
        {
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30f, 1f)
        };

        narrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;


        _staticHandles[entityId] = (handle, shape);
    }

    public void RemoveDynamicBody(EntityId entityId)
    {
        var (handle, shape) = _dynamicHandles[entityId];
        _dynamicHandles.Remove(entityId);
        _simulation.Bodies.Remove(handle);
        _simulation.Shapes.Remove(shape);
    }

    public void AddDynamicBody(EntityId entityId, GlobalTransform transform, float bounciness)
    {
        if (_simulation.NarrowPhase is not NarrowPhase<MyNarrowPhaseCallback> narrowPhase)
        {
            return;
        }

        var (position, rotation, _) = transform.GetPositionRotationScale();

        var shape = new Box(transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
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

        narrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;

        _dynamicHandles.Add(entityId, (handle, shapeIndex));
    }

    public void AddKinematicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider)
    {
        var (shapeIndex, _) = AddColliderAsShape(collider, transform.Scale, 10f);

        var (position, rotation, _) = transform.GetPositionRotationScale();

        var body = BodyDescription.CreateKinematic(
            new RigidPose(position, rotation),
            new BodyVelocity(),
            new CollidableDescription(shapeIndex),
            new BodyActivityDescription(0.01f));

        var handle = _simulation.Bodies.Add(body);

        _dynamicHandles.Add(entityId, (handle, shapeIndex));
    }

    public void AddDynamicBody2D(EntityId entityId, GlobalTransform transform, ICollider2D collider, float bounciness)
    {
        if (_simulation.NarrowPhase is not NarrowPhase<MyNarrowPhaseCallback> narrowPhase)
        {
            return;
        }

        var (shapeIndex, inertia) = AddColliderAsShape(collider, transform.Scale, 10f);
        var inverseInertiaTensor = inertia.InverseInertiaTensor;

        // dont allow rotation along X or Y Axis for 2D
        inverseInertiaTensor.XX = 0f;
        inverseInertiaTensor.YY = 0f;

        inertia.InverseInertiaTensor = inverseInertiaTensor;

        var (position, rotation, _) = transform.GetPositionRotationScale();

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

        narrowPhase.Callbacks.CollidableMaterials.Allocate(handle) = material;


        _dynamicHandles.Add(entityId, (handle, shapeIndex));
    }

    public void ApplyImpulse(EntityId entityId, Vector3 impulse)
    {
        var (handle, _) = _dynamicHandles[entityId];
        var bodyReference = _simulation.Bodies[handle];
        bodyReference.Awake = true;
        bodyReference.ApplyLinearImpulse(impulse);
    }

    public void ApplyAngularImpulse(EntityId entityId, Vector3 impulse)
    {
        var (handle, _) = _dynamicHandles[entityId];
        var bodyReference = _simulation.Bodies[handle];
        bodyReference.Awake = true;
        bodyReference.ApplyAngularImpulse(impulse);
    }

    public (Vector3 Position, Quaternion Rotation) GetDynamicPhysicsTransform(EntityId entityId)
    {
        var (handle, _) = _dynamicHandles[entityId];
        var body = _simulation.Bodies[handle];
        var pose = body.Pose;

        return (pose.Position, pose.Orientation);
    }

    public void ApplyDynamicPhysicsTransform(EntityId entityId, GlobalTransform transform)
    {
        var (handle, _) = _dynamicHandles[entityId];
        var body = _simulation.Bodies[handle];

        body.GetDescription(out var description);

        var (position, rotation, _) = transform.GetPositionRotationScale();

        description.Pose.Position = position;
        description.Pose.Orientation = rotation;
        body.ApplyDescription(description);
    }

    public void ApplyStaticPhysicsTransform(EntityId entityId, GlobalTransform transform)
    {
        var (handle, _) = _staticHandles[entityId];
        var body = _simulation.Statics[handle];

        var (position, rotation, _) = transform.GetPositionRotationScale();

        body.GetDescription(out var description);

        description.Pose.Position = position;
        description.Pose.Orientation = rotation;
        body.ApplyDescription(description);
    }
}
