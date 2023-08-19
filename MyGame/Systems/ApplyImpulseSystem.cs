using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;
using System.Numerics;

namespace MyGame.Systems;

public class ApplyImpulseSystem : ISystem
{
    private readonly PhysicsResource _physicsResource;
    private readonly InputResource _inputResource;
    private readonly IQuery<BallComponent> _query;

    public ApplyImpulseSystem(PhysicsResource physicsResource, InputResource inputResource, IQuery<BallComponent> query)
    {
        _physicsResource = physicsResource;
        _inputResource = inputResource;
        _query = query;
    }

    public void Run(double deltaTime)
    {
        if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.T))
        {
            var player = _query.FirstOrDefault();
            if (player is not null)
            {
                _physicsResource.ApplyImpulse(player.EntityId, new Vector3(1f, 1f, 0f) * 3f);
            }
        }
    }
}
