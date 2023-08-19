using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyGame.Systems;

public class OnCollisionSystem : ISystem
{
    private readonly CollisionsResource _collisionsResource;

    public OnCollisionSystem(CollisionsResource collisionsResource)
    {
        _collisionsResource = collisionsResource;
    }

    public void Run(double deltaTime)
    {
        foreach (var collision in _collisionsResource.NewCollisions)
        {
            Console.WriteLine("Collision between {0} and {1}", collision.EntityA.Value, collision.EntityB.Value);
        }
    }
}
