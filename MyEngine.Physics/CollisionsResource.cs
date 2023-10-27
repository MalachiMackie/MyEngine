using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Physics;


public class Collision
{
    public required EntityId EntityA { get; init; }

    public required Vector3? EntityAVelocity { get; init; }

    public required EntityId EntityB { get; init; }

    public required Vector3? EntityBVelocity { get; init; }

    public required Vector3 Normal { get; init; }
}

public class CollisionsResource : IResource
{
    internal List<Collision> _newCollisions = new();
    internal List<Collision> _existingCollisions = new();
    internal List<Collision> _oldCollisions = new();

    // todo: components
    public IReadOnlyList<Collision> NewCollisions => _newCollisions;

    public IReadOnlyList<Collision> ExistingCollisions => _existingCollisions;

    public IReadOnlyList<Collision> OldCollisions => _oldCollisions;
}
