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
                    position = new Vector3(),
                    rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 15),
                    scale = Vector3.One
                },
                new Transform
                {
                    position = new Vector3(-0.75f, -0.75f, 0),
                    rotation = Quaternion.Identity,
                    scale = new Vector3(0.25f)
                },
                new Transform
                {
                    position = new Vector3(-0.5f, 0, -0.1f),
                    rotation = Quaternion.Identity,
                    scale = Vector3.One
                },
                new Transform
                {
                    position = new Vector3(0.5f, 0, 0),
                    rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 45),
                    scale = Vector3.One
                }
            };

            foreach (var transform in transforms)
            {
                var entity = new Entity();
                _entityContainerResource.AddEntity(entity);
                _componentContainerResource.AddComponent(new TransformComponent(entity.Id, transform));
                _componentContainerResource.AddComponent(new SpriteComponent(entity.Id));
            }
        }
    }
}
