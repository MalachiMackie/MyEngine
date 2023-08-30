using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs;

public interface IEntityBuilderTransformStep
{
    public IEntityBuilderDisplayStep WithTransform(Transform transform);

    public IEntityBuilderDisplayStep WithDefaultTransform();
}

public interface IEntityBuilderDisplayStep
{
    public IEntityBuilderPhysicsStep WithNoDisplay();

    public IEntityBuilderPhysicsStep WithSprite();
}

public interface IEntityBuilderPhysicsStep
{
    public IEntityBuilder WithoutPhysics();

    public IEntityBuilderPhysicsCollider2DStep WithKinematic2DPhysics();

    public IEntityBuilderPhysicsCollider2DStep WithDynamic2DPhysics();

    public IEntityBuilderPhysicsCollider2DStep WithStatic2DPhysics();
} 


public interface IEntityBuilderPhysicsCollider2DStep
{
    public IEntityBuilder WithBox2DCollider(Vector2 dimensions);

    /// <summary>
    /// Add a circle collider to the entity.
    /// 
    /// Note: radius is in world scale, not relative to its parent
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public IEntityBuilder WithCircle2DCollider(float radius);
}

public interface IEntityBuilder
{
    public IEntityBuilder WithComponent(IComponent component);

    public IReadOnlyCollection<IComponent> Build();
}

public class EntityBuilder :
    IEntityBuilder,
    IEntityBuilderTransformStep,
    IEntityBuilderDisplayStep,
    IEntityBuilderPhysicsCollider2DStep,
    IEntityBuilderPhysicsStep,
    IEntityBuilderKinematicReboundStep
{
    private EntityBuilder()
    {

    }

    private readonly List<IComponent> _components = new();

    public IReadOnlyCollection<IComponent> Build()
    {
        return _components;
    }

    public IEntityBuilder WithComponent(IComponent component)
    {
        _components.Add(component);
        return this;
    }

    public IEntityBuilderDisplayStep WithDefaultTransform()
    {
        var transform = Transform.Default();
        _components.Add(new TransformComponent(transform));
        return this;
    }

    public IEntityBuilderDisplayStep WithTransform(Transform transform)
    {
        _components.Add(new TransformComponent(transform));
        return this;
    }

    public static IEntityBuilderTransformStep Create()
    {
        return new EntityBuilder();
    }

    public IEntityBuilderPhysicsStep WithNoDisplay()
    {
        return this;
    }

    public IEntityBuilderPhysicsStep WithSprite()
    {
        _components.Add(new SpriteComponent());
        return this;
    }

    public IEntityBuilder WithoutPhysics()
    {
        return this;
    }

    public IEntityBuilderKinematicReboundStep WithKinematic2DPhysics()
    {
        _components.Add(new KinematicBody2DComponent());
        return this;
    }

    public IEntityBuilderPhysicsCollider2DStep WithDynamic2DPhysics()
    {
        _components.Add(new DynamicBody2DComponent());
        return this;
    }

    public IEntityBuilderPhysicsCollider2DStep WithStatic2DPhysics()
    {
        _components.Add(new StaticBody2DComponent());
        return this;
    }

    public IEntityBuilder WithBox2DCollider(Vector2 dimensions)
    {
        _components.Add(new Collider2DComponent(new BoxCollider2D(dimensions)));
        return this;
    }

    public IEntityBuilder WithCircle2DCollider(float radius)
    {
        _components.Add(new Collider2DComponent(new CircleCollider2D(radius)));
        return this;
    }

    public IEntityBuilderPhysicsCollider2DStep WithoutRebound()
    {
        return this;
    }

    public IEntityBuilderPhysicsCollider2DStep WithRebound()
    {
        _components.Add(new KinematicReboundComponent());
        return this;
    }
}
