using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System.Numerics;

namespace MyGame.Systems
{
    public class AddStartupSpritesSystem : IStartupSystem
    {
        private readonly EntityContainerResource _entityContainerResource;
        private readonly ComponentContainerResource _componentContainerResource;

        public AddStartupSpritesSystem(EntityContainerResource entityContainerResource,
            ComponentContainerResource componentContainerResource)
        {
            _entityContainerResource = entityContainerResource;
            _componentContainerResource = componentContainerResource;
        }

        public void Run()
        {
            var transforms = new[]
            {
                new Transform {
                    position = new Vector3(0f, 2f, 0f),
                    rotation = Quaternion.Identity,
                    scale = Vector3.One
                },
                new Transform
                {
                    position = new Vector3(-4f, 0f, 0f),
                    rotation = Quaternion.Identity,
                    scale = new Vector3(0.1f, 4.5f, 1f)
                },
                new Transform
                {
                    position = new Vector3(4f, 0f, 0f),
                    rotation = Quaternion.Identity,
                    scale = new Vector3(0.1f, 4.5f, 1f)
                },
                new Transform
                {
                    position = new Vector3(0f, 2.25f, 0f),
                    rotation = Quaternion.Identity,
                    scale = new Vector3(8f, 0.1f, 1f)
                },
                new Transform
                {
                    position = new Vector3(0f, -2.25f, 0f),
                    rotation = Quaternion.Identity,
                    scale = new Vector3(8f, 0.1f, 1f) 
                }
            };

            foreach (var transform in transforms)
            {
                var entity = EntityId.Generate();
                _entityContainerResource.AddEntity(entity);
                _componentContainerResource.AddComponent(new TransformComponent(entity, transform));
                _componentContainerResource.AddComponent(new SpriteComponent(entity));
                _componentContainerResource.AddComponent(new StaticBody2DComponent(entity));
                _componentContainerResource.AddComponent(new Collider2DComponent(entity, new BoxCollider2D(Vector2.One)));
                // _componentContainerResource.AddComponent(new TestComponent(entity.Id));
                _componentContainerResource.AddComponent(new PhysicsMaterial(entity, 0f));
            }

            var playerEntity = EntityId.Generate();
            _entityContainerResource.AddEntity(playerEntity);
            _componentContainerResource.AddComponent(new PlayerComponent(playerEntity));
            _componentContainerResource.AddComponent(new SpriteComponent(playerEntity));
            _componentContainerResource.AddComponent(new TransformComponent(playerEntity, new Transform
            {
                position = new Vector3(0f, -1f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.25f, 0.25f, 1f)
            }));
            _componentContainerResource.AddComponent(new DynamicBody2DComponent(playerEntity));
            _componentContainerResource.AddComponent(new Collider2DComponent(playerEntity, new CircleCollider2D(1f)));
            _componentContainerResource.AddComponent(new PhysicsMaterial(playerEntity, 1f));
        }
    }
}
