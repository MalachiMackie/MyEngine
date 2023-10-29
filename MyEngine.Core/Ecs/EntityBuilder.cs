using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core.Ecs;

public interface ITransformStepEntityBuilder
{
    public ICompleteStepEntityBuilder WithTransform(Transform transform);
    public ICompleteStepEntityBuilder WithDefaultTransform(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null);
}

public interface ICompleteStepEntityBuilder
{
    ICompleteStepEntityBuilder WithComponent(IComponent component);

    ICompleteStepEntityBuilder WithComponents(params IComponent[] components);

    ICompleteStepEntityBuilder WithChild(Func<ITransformStepEntityBuilder, ICompleteStepEntityBuilder> childBuilder);

    internal record struct BuildResult(IReadOnlyList<IComponent> Components, IReadOnlyList<ICompleteStepEntityBuilder> Children);

    internal BuildResult Build();
}

public class EntityBuilder : ITransformStepEntityBuilder, ICompleteStepEntityBuilder
{
    private readonly List<IComponent> _components = new();
    private readonly List<ICompleteStepEntityBuilder> _children = new();

    public static ITransformStepEntityBuilder Create()
    {
        return new EntityBuilder();
    }

    public ICompleteStepEntityBuilder WithChild(Func<ITransformStepEntityBuilder, ICompleteStepEntityBuilder> childBuilder)
    {
        _children.Add(childBuilder(Create()));
        return this;
    }

    public ICompleteStepEntityBuilder WithComponent(IComponent component)
    {
        _components.Add(component);
        return this;
    }

    public ICompleteStepEntityBuilder WithComponents(params IComponent[] components)
    {
        _components.AddRange(components);
        return this;
    }

    public ICompleteStepEntityBuilder WithTransform(Transform transform)
    {
        _components.Add(new TransformComponent(transform));
        return this;
    }

    public ICompleteStepEntityBuilder WithDefaultTransform(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        _components.Add(new TransformComponent(Transform.Default(position, scale, rotation)));
        return this;
    }

    ICompleteStepEntityBuilder.BuildResult ICompleteStepEntityBuilder.Build()
    {
        return new(_components, _children);
    }
}
