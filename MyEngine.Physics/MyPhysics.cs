using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Memory;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using System.Numerics;

namespace MyEngine.Physics
{
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

    internal struct MyNarrowPhaseCallback : INarrowPhaseCallbacks
    {
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = 1f;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1);

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
        }
    }

    public class MyPhysics : IResource
    {
        private readonly Simulation _simulation;
        private readonly BufferPool _bufferPool;

        private Dictionary<EntityId, (StaticHandle, TypedIndex)> _staticHandles = new();
        private Dictionary<EntityId, (BodyHandle, TypedIndex)> _dynamicHandles = new();

        public MyPhysics()
        {
            _bufferPool = new BufferPool();
            _simulation = Simulation.Create(_bufferPool,
                new MyNarrowPhaseCallback(),
                new MyPoseIntegratorCallbacks(),
                new SolveDescription(6, 1));
        }

        public void Update(double dt)
        {
            _simulation.Timestep((float)dt);
        }

        public void RemoveStaticBody(EntityId entityId)
        {
            var (handle, shape) = _staticHandles[entityId];
            _staticHandles.Remove(entityId);
            _simulation.Statics.Remove(handle);
            _simulation.Shapes.Remove(shape);
        }

        public void AddStaticBody(EntityId entityId, Transform transform)
        {
            var shape = _simulation.Shapes.Add(new Box(transform.scale.X, transform.scale.Y, transform.scale.Z));
            var handle = _simulation.Statics.Add(new StaticDescription(transform.position, transform.rotation, shape));
            _staticHandles[entityId] = (handle, shape);
        }

        public void AddStaticBody2D(EntityId entityId, Transform transform)
        {
            var shape = _simulation.Shapes.Add(new Box(transform.scale.X, transform.scale.Y, 1000f));
            var handle = _simulation.Statics.Add(new StaticDescription(transform.position, transform.rotation, shape));
            _staticHandles[entityId] = (handle, shape);
        }

        public void RemoveDynamicBody(EntityId entityId)
        {
            var (handle, shape) = _dynamicHandles[entityId];
            _dynamicHandles.Remove(entityId);
            _simulation.Bodies.Remove(handle);
            _simulation.Shapes.Remove(shape);
        }

        public void AddDynamicBody(EntityId entityId, Transform transform)
        {
            var shape = new Box(transform.scale.X, transform.scale.Y, transform.scale.Z);
            var shapeIndex = _simulation.Shapes.Add(shape);
            var handle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose(transform.position, transform.rotation),
                new BodyVelocity(new Vector3(0f, 0f, 0f)),
                shape.ComputeInertia(10f),
                new CollidableDescription(shapeIndex),
                new BodyActivityDescription(0.01f)));

            _dynamicHandles.Add(entityId, (handle, shapeIndex));
        }

        public void AddDynamicBody2D(EntityId entityId, Transform transform)
        {
            var shape = new Box(transform.scale.X, transform.scale.Y, transform.scale.Z);
            var shapeIndex = _simulation.Shapes.Add(shape);

            var inertia = shape.ComputeInertia(10f);
            var inverseInertiaTensor = inertia.InverseInertiaTensor;

            // dont allow rotation along X or Y Axis for 2D
            inverseInertiaTensor.XX = 0f;
            inverseInertiaTensor.YY = 0f;

            inertia.InverseInertiaTensor = inverseInertiaTensor;

            var body = BodyDescription.CreateDynamic(
                new RigidPose(transform.position, transform.rotation),
                new BodyVelocity(),
                inertia, // todo: inertia probably needs to be handled differently for 2d
                new CollidableDescription(shapeIndex),
                new BodyActivityDescription(0.01f));

            var handle = _simulation.Bodies.Add(body);

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

        public void UpdateStaticTransform(EntityId entityId, Transform transform)
        {
            var (handle, _) = _staticHandles[entityId];
            var pose = _simulation.Statics[handle].Pose;

            transform.position = pose.Position;
            transform.rotation = pose.Orientation;
        }

        public void UpdateDynamicTransform(EntityId entityId, Transform transform)
        {
            var (handle, _) = _dynamicHandles[entityId];
            var pose = _simulation.Bodies[handle].Pose;

            transform.position = pose.Position;
            transform.rotation = pose.Orientation;
        }
    }
}
