using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Resources;
using System.Numerics;

namespace MyGame.Systems;

public class AddStartupSpritesSystem : IStartupSystem
{
    private readonly EntityContainerResource _entityContainerResource;
    private readonly ComponentContainerResource _componentContainerResource;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;

    public AddStartupSpritesSystem(EntityContainerResource entityContainerResource,
        ComponentContainerResource componentContainerResource,
        ResourceRegistrationResource resourceRegistrationResource)
    {
        _entityContainerResource = entityContainerResource;
        _componentContainerResource = componentContainerResource;
        _resourceRegistrationResource = resourceRegistrationResource;
    }

    public void Run()
    {
        var walls = new[]
        {
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
        };
        _resourceRegistrationResource.AddResource(new WorldSizeResource {
            Bottom = -2.5f,
            Top = 2.5f,
            Left = -4.25f,
            Right = 4.25f
        });

        foreach (var transform in walls)
        {
            var entity = EntityId.Generate();
            _entityContainerResource.AddEntity(entity);
            _componentContainerResource.AddComponent(entity, new TransformComponent(transform));
            _componentContainerResource.AddComponent(entity, new SpriteComponent());
            _componentContainerResource.AddComponent(entity, new StaticBody2DComponent());
            _componentContainerResource.AddComponent(entity, new Collider2DComponent(new BoxCollider2D(Vector2.One)));
        }

        var ballEntity = EntityId.Generate();
        _entityContainerResource.AddEntity(ballEntity);
        _componentContainerResource.AddComponent(ballEntity, new BallComponent());
        _componentContainerResource.AddComponent(ballEntity, new SpriteComponent());
        _componentContainerResource.AddComponent(ballEntity, new TransformComponent(new Transform
        {
            position = new Vector3(0f, -1f, 0f),
            rotation = Quaternion.Identity,
            scale = new Vector3(0.25f, 0.25f, 1f)
        }));
        _componentContainerResource.AddComponent(ballEntity, new KinematicBody2DComponent());
        _componentContainerResource.AddComponent(ballEntity, new KinematicReboundComponent());
        _componentContainerResource.AddComponent(ballEntity, new Collider2DComponent(new CircleCollider2D(1f)));
    }
}
