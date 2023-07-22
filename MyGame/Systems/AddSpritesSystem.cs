﻿using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyGame.Systems
{
    public class AddSpritesSystem : ISystem
    {
        private readonly InputResource _inputResource;
        private readonly EntityContainerResource _entityContainerResource;
        private readonly ComponentContainerResource _componentContainerResource;

        private readonly MyQuery<SpriteComponent, TransformComponent> _query;

        public AddSpritesSystem(InputResource inputResource,
            EntityContainerResource entityContainerResource,
            ComponentContainerResource componentContainerResource,
            MyQuery<SpriteComponent, TransformComponent> query)
        {
            _inputResource = inputResource;
            _entityContainerResource = entityContainerResource;
            _componentContainerResource = componentContainerResource;
            _query = query;
        }

        public void Run(double deltaTime)
        {
            if (!_inputResource.Keyboard.IsKeyPressed(MyKey.Space))
            {
                return;
            }

            // get far left sprite
            var minTransform = _query.Select(x => x.Item2.Transform)
                .MinBy(x => x.position.X);

            if (minTransform is null)
            {
                // \(o_o)/
                return;
            }

            var newEntity = new Entity();
            var newTransformComponent = new TransformComponent(newEntity.Id, new MyEngine.Core.Transform
            {
                position = minTransform.position,
                rotation = minTransform.rotation,
                scale = minTransform.scale
            });

            ref var position = ref newTransformComponent.Transform.position;
            position.X -= 1;

            _entityContainerResource.AddEntity(newEntity);
            _componentContainerResource.AddComponent(newTransformComponent);
            _componentContainerResource.AddComponent(new SpriteComponent(newEntity.Id));
        }
    }
}