using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs;

public interface IEntityBuilderTransformStep
{
    public IEntityBuilder WithTransform(Transform transform);

    public IEntityBuilder WithDefaultTransform();
}

public interface IEntityBuilder
{
    public IEntityBuilder WithComponent(IComponent component);

    public IReadOnlyCollection<IComponent> Build();
}

public class EntityBuilder : IEntityBuilder, IEntityBuilderTransformStep
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

    public IEntityBuilder WithDefaultTransform()
    {
        _components.Add(new TransformComponent());
        return this;
    }

    public IEntityBuilder WithTransform(Transform transform)
    {
        _components.Add(new TransformComponent(transform));
        return this;
    }

    public static IEntityBuilderTransformStep Create()
    {
        return new EntityBuilder();
    }
}
