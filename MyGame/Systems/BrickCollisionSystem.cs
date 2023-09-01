using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Physics;
using MyGame.Components;

namespace MyGame.Systems;

public class BrickCollisionSystem : ISystem
{
    private readonly ICommands _commands;
    private readonly CollisionsResource _collisions;
    private readonly IQuery<BallComponent> _ballsQuery;
    private readonly IQuery<BrickComponent> _bricksQuery;

    public BrickCollisionSystem(CollisionsResource collisions, IQuery<BallComponent> ballsQuery, IQuery<BrickComponent> bricksQuery, ICommands commands)
    {
        _collisions = collisions;
        _ballsQuery = ballsQuery;
        _bricksQuery = bricksQuery;
        _commands = commands;
    }

    public void Run(double deltaTime)
    {
        foreach (var ball in _ballsQuery)
        {
            var collidedWith = GetCollisionsWith(ball.EntityId);
            var bricksCollidedWith = collidedWith.Where(x => _bricksQuery.TryGetForEntity(x) is not null);

            foreach (var brick in bricksCollidedWith)
            {
                _commands.RemoveEntity(brick);
            }
        }
    }

    private IEnumerable<EntityId> GetCollisionsWith(EntityId entity)
    {
        var isEntityA = _collisions.NewCollisions.Where(x => x.EntityA == entity)
            .Select(x => x.EntityB);

        var isEntityB = _collisions.NewCollisions.Where(x => x.EntityB == entity)
            .Select(x => x.EntityA);

        return isEntityA.Concat(isEntityB);
    }
}
