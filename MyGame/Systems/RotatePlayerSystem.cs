using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyGame.Systems
{
    public class RotatePlayerSystem : ISystem
    {
        private readonly MyQuery<PlayerComponent> _playerQuery;
        private readonly PhysicsResource _physicsResource;
        private readonly InputResource _inputResource;

        public RotatePlayerSystem(MyQuery<PlayerComponent> playerQuery, PhysicsResource physicsResource, InputResource inputResource)
        {
            _physicsResource = physicsResource;
            _playerQuery = playerQuery;
            _inputResource = inputResource;

        }

        public void Run(double deltaTime)
        {
            if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.Q))
            {
                _physicsResource.ApplyAngularImpulse(_playerQuery.First().EntityId, new Vector3(0f, 0f, 0.1f));
            }
            else if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.E))
            {
                _physicsResource.ApplyAngularImpulse(_playerQuery.First().EntityId, new Vector3(0f, 0f, -0.1f));
            }
        }
    }
}
