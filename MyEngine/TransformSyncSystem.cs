using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime;

/// <summary>
///  System to sync local Transform changes up to GlobalTransform
/// </summary>
internal class TransformSyncSystem : ISystem
{
    private readonly IQuery<TransformComponent, OptionalComponent<ParentComponent>, OptionalComponent<ChildrenComponent>> _query;

    public TransformSyncSystem(IQuery<TransformComponent, OptionalComponent<ParentComponent>, OptionalComponent<ChildrenComponent>> query)
    {
        _query = query;
    }

    public void Run(double deltaTime)
    {
        foreach (var components in _query.Where(x => !x.Component2.HasComponent))
        {
            var (transform, _, children) = components;
            transform.GlobalTransform.SetWithTransform(transform.LocalTransform);

            if (children.HasComponent)
            {
                SyncChildren(transform.GlobalTransform, children.Component.Children);
            }
        }
    }

    private void SyncChildren(GlobalTransform parentTransform, IEnumerable<EntityId> children)
    {
        foreach (var child in children)
        {
            var (transform, _, grandChildren) = _query.TryGetForEntity(child)!;

            transform.GlobalTransform.SetWithParentTransform(parentTransform, transform.LocalTransform);

            if (grandChildren.HasComponent)
            {
                SyncChildren(transform.GlobalTransform, grandChildren.Component.Children);
            }
        }
    }
}
