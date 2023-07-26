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
            };

            foreach (var transform in transforms)
            {
                var entity = new Entity();
                _entityContainerResource.AddEntity(entity);
                _componentContainerResource.AddComponent(new TransformComponent(entity.Id, transform));
                _componentContainerResource.AddComponent(new SpriteComponent(entity.Id));
                _componentContainerResource.AddComponent(new StaticBody2DComponent(entity.Id));
                _componentContainerResource.AddComponent(new BoxCollider2DComponent(entity.Id, Vector2.One));
            }

            var playerEntity = new Entity();
            _entityContainerResource.AddEntity(playerEntity);
            _componentContainerResource.AddComponent(new PlayerComponent(playerEntity.Id));
            _componentContainerResource.AddComponent(new SpriteComponent(playerEntity.Id));
            _componentContainerResource.AddComponent(new TransformComponent(playerEntity.Id, new Transform
            {
                position = new Vector3(0f, -1f, 0f),
                rotation = Quaternion.Identity,
                scale = new Vector3(0.25f, 0.25f, 1f)
            }));
            _componentContainerResource.AddComponent(new DynamicBody2DComponent(playerEntity.Id));
            _componentContainerResource.AddComponent(new BoxCollider2DComponent(playerEntity.Id, Vector2.One));
        }
    }
}
