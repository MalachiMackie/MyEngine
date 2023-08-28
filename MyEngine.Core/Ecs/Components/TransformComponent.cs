namespace MyEngine.Core.Ecs.Components;

public class TransformComponent : IComponent
{
    public TransformComponent(Transform transform)
    {
        LocalTransform = transform;
        GlobalTransform = GlobalTransform.FromTransform(transform);
    }

    public Transform LocalTransform { get; }

    public GlobalTransform GlobalTransform { get; }
}
