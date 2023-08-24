using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime;

internal class TransformSyncSystem : ISystem
{
    private readonly IQuery<TransformComponent, OptionalComponent<ParentComponent>, OptionalComponent<ChildrenComponent>> _query;

    public TransformSyncSystem(IQuery<TransformComponent, OptionalComponent<ParentComponent>, OptionalComponent<ChildrenComponent>> query)
    {
        _query = query;
    }

    public void Run(double deltaTime)
    {
        // update all of the transforms without parents, then update their children
        foreach (var components in _query.Where(x => !x.Component2.HasComponent))
        {
            var (transform, _, children) = components;
            transform.GlobalTransform.SyncWithLocalTransform(null, transform.LocalTransform);

            UpdateChildrenTransforms(transform.GlobalTransform, children);
        }
    }

    private void UpdateChildrenTransforms(GlobalTransform parentTransform, OptionalComponent<ChildrenComponent> children)
    {
        if (!children.HasComponent)
        {
            return;
        }

        foreach (var child in children.Component.Children)
        {
            var childComponents = _query.TryGetForEntity(child);
            if (childComponents is null)
            {
                throw new InvalidOperationException("Entity is missing transform");
            }

            var (transform, _, grandChildren) = childComponents;
            transform.GlobalTransform.SyncWithLocalTransform(parentTransform, transform.LocalTransform);

            UpdateChildrenTransforms(transform.GlobalTransform, grandChildren);
        }
    }
}
