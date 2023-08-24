namespace MyEngine.Core.Ecs.Components;

public class TransformComponent : IComponent
{
    public TransformComponent(Transform transform)
    {
        LocalTransform = transform;
        GlobalTransform = GlobalTransform.FromTransform(transform);
    }

    public Transform LocalTransform { get; set; }
    public GlobalTransform GlobalTransform { get; }

    private GlobalTransform CalculateGlobalTransform(GlobalTransform? parentGlobalTransform, Transform localTransform)
    {
        // if we have no parent transform, then our local transform is global
        if (parentGlobalTransform is null)
        {
            return GlobalTransform.FromTransform(localTransform);
        }

        var globalPosition = parentGlobalTransform.position + localTransform.position;
        var globalRotation = parentGlobalTransform.rotation * localTransform.rotation;
        var globalScale = parentGlobalTransform.scale * localTransform.scale;

        return new GlobalTransform(globalPosition, globalRotation, globalScale);
    }

    private Transform CalculateLocalTransform(GlobalTransform? parentGlobalTransform, GlobalTransform globalTransform)
    {
        // if we have to parent transform, then our global transform is local
        if (parentGlobalTransform is null)
        {
            return new Transform(globalTransform.position, globalTransform.rotation, globalTransform.scale);
        }

        var localPosition = globalTransform.position - parentGlobalTransform.position;
        // apply global rotation, then inverse parent rotation
        // quaternions get applied/rotated right to left
        // localRotation = parentRotation.Inversed * globalRotation
        var localRotation = Quaternion.Inverse(parentGlobalTransform.rotation) * globalTransform.rotation;
        var localScale = globalTransform.scale / parentGlobalTransform.scale;

        return new Transform(localPosition, localRotation, localScale);
    }
}
