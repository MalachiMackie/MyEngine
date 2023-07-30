using MyEngine.Core.Ecs;
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
    public class ApplyImpulseSystem : ISystem
    {
        private readonly PhysicsResource _physicsResource;
        private readonly InputResource _inputResource;
        private readonly MyQuery<PlayerComponent> _query;

        public ApplyImpulseSystem(PhysicsResource physicsResource, InputResource inputResource, MyQuery<PlayerComponent> query)
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
                    _physicsResource.ApplyImpulse(player.EntityId, Vector3.UnitY * 3f);
                }
            }
        }
    }
}
