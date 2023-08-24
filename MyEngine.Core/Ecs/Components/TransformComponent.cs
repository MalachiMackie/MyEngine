namespace MyEngine.Core.Ecs.Components;

public class TransformComponent : IComponent
{
    public TransformComponent(GlobalTransform? parentTransform, Transform localTransform)
    {
        ParentGlobalTransform = parentTransform;
        GlobalTransform = CalculateGlobalTransform(parentTransform, localTransform);
    }

    public TransformComponent(GlobalTransform? parentTransform, GlobalTransform globalTransform)
    {
        ParentGlobalTransform = parentTransform;
        GlobalTransform = globalTransform;
    }

    public GlobalTransform? ParentGlobalTransform { get; }

    public GlobalTransform GlobalTransform { get; }

    public Transform LocalTransform
    {
        get => CalculateLocalTransform(ParentGlobalTransform, GlobalTransform);
        set
        {
            var calculatedGlobalTransform = CalculateGlobalTransform(ParentGlobalTransform, localTransform: value);

            GlobalTransform.position = calculatedGlobalTransform.position;
            GlobalTransform.rotation = calculatedGlobalTransform.rotation;
            GlobalTransform.scale = calculatedGlobalTransform.scale;
        }
    }

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

        return new GlobalTransform
        {
            position = globalPosition,
            rotation = globalRotation,
            scale = globalScale
        };
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

        return new Transform
        {
            position = localPosition,
            rotation = localRotation,
            scale = localScale
        };
    }
}
