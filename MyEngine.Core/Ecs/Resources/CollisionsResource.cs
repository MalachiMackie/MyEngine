namespace MyEngine.Core.Ecs.Resources;


public class Collision
{
    public required EntityId EntityA { get; init; }
    public required EntityId EntityB { get; init; }
}

public class CollisionsResource : IResource
{
    internal List<Collision> _newCollisions = new();

    public IReadOnlyList<Collision> NewCollisions => _newCollisions;
}
