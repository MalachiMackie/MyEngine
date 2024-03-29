using System.Diagnostics.CodeAnalysis;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Runtime;

internal class ResourceContainer : IResourceContainer
{
    private readonly Dictionary<Type, object> _resources = [];

    public bool TryGetResource<T>([NotNullWhen(true)] out T? resource) where T : class, IResource
    {
        if (!_resources.TryGetValue(typeof(T), out var resourceObj))
        {
            resource = null;
            return false;
        }

        resource = resourceObj as T;
        return resource is not null;
    }

    public Result<Unit> RegisterResource<T>(T resource) where T : IResource
    {
        var resourceType = typeof(T);
        if (!_resources.TryAdd(resourceType, resource))
        {
            return Result.Failure<Unit>($"Resource of type {resourceType.Name} has already been added to the resource container");
        }

        return Result.Success<Unit>(Unit.Value);
    }

    public Result<Unit> RegisterResource(Type resourceType, IResource resource)
    {
        if (!resourceType.IsAssignableTo(typeof(IResource)))
        {
            return Result.Failure<Unit>($"Resource of type {resourceType.Name} does not implement {nameof(IResource)}");
        }

        if (!_resources.TryAdd(resourceType, resource))
        {
            return Result.Failure<Unit>($"Resource of type {resourceType.Name} has already been added to the resource container");
        }

        return Result.Success<Unit>(Unit.Value);
    }
}
