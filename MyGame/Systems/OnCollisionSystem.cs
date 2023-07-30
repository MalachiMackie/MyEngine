using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGame.Systems
{
    public class OnCollisionSystem : ISystem
    {
        private readonly CollisionsResource _collisionsResource;
        private readonly EntityContainerResource _entityContainerResource;
        private readonly MyQuery<TestComponent> _testComponentQuery;

        public OnCollisionSystem(CollisionsResource collisionsResource,
            MyQuery<TestComponent> testComponentQuery,
            EntityContainerResource entityContainerResource)
        {
            _collisionsResource = collisionsResource;
            _testComponentQuery = testComponentQuery;
            _entityContainerResource = entityContainerResource;
        }

        public void Run(double deltaTime)
        {
            var testComponents = _testComponentQuery.ToArray();
            foreach (var collision in _collisionsResource.NewCollisions)
            {
                Console.WriteLine("Collision between {0} and {1}", collision.EntityA.Value, collision.EntityB.Value);
                if (testComponents.Any(x => x.EntityId == collision.EntityA))
                {
                    _multiFrameProcessing.Add(RemoveEntityNextFrame(collision.EntityA));
                }
                else if (testComponents.Any(x => x.EntityId == collision.EntityB))
                {
                    _multiFrameProcessing.Add(RemoveEntityNextFrame(collision.EntityB));
                }
            }

            // todo: engine integration of multi frame processing
            for (int i = _multiFrameProcessing.Count - 1; i >= 0; i--)
            {
                var enumerator = _multiFrameProcessing[i];
                if (!enumerator.MoveNext())
                {
                    _multiFrameProcessing.RemoveAt(i);
                }
            }
        }

        private IEnumerator RemoveEntityNextFrame(EntityId entityId)
        {
            // wait a few frames before removing the entity
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            _entityContainerResource.RemoveEntity(entityId);
        }

        private readonly List<IEnumerator> _multiFrameProcessing = new();
    }
}
