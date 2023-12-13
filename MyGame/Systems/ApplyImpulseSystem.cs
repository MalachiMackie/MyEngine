using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Physics;
using MyGame.Components;
using System.Numerics;

namespace MyGame.Systems;

public class ApplyImpulseSystem : ISystem
{
    private readonly PhysicsResource _physicsResource;
    private readonly IQuery<BallComponent> _query;
    private readonly IKeyboard _keyboard;

    public ApplyImpulseSystem(PhysicsResource physicsResource, IQuery<BallComponent> query, IKeyboard keyboard)
    {
        _physicsResource = physicsResource;
        _query = query;
        _keyboard = keyboard;
    }

    public void Run(double deltaTime)
    {
        if (_keyboard.IsKeyPressed(MyKey.T))
        {
            var player = _query.FirstOrDefault();
            if (player is not null)
            {
                _physicsResource.ApplyImpulse(player.EntityId, new Vector3(1f, 1f, 0f) * 3f);
            }
        }
    }
}
