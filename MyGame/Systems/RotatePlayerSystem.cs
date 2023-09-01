using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Physics;
using MyGame.Components;

namespace MyGame.Systems;

public class RotatePlayerSystem : ISystem
{
    private readonly IQuery<BallComponent> _playerQuery;
    private readonly PhysicsResource _physicsResource;
    private readonly InputResource _inputResource;

    public RotatePlayerSystem(IQuery<BallComponent> playerQuery, PhysicsResource physicsResource, InputResource inputResource)
    {
        _physicsResource = physicsResource;
        _playerQuery = playerQuery;
        _inputResource = inputResource;

    }

    public void Run(double deltaTime)
    {
        if (_inputResource.Keyboard.IsKeyPressed(MyKey.Q))
        {
            // _physicsResource.ApplyAngularImpulse(_playerQuery.First().EntityId, new Vector3(0f, 0f, 0.1f));
        }
        else if (_inputResource.Keyboard.IsKeyPressed(MyKey.E))
        {
            // _physicsResource.ApplyAngularImpulse(_playerQuery.First().EntityId, new Vector3(0f, 0f, -0.1f));
        }
    }
}
