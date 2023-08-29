using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;
using System.Diagnostics.CodeAnalysis;

namespace MyEngine.Runtime;

internal enum RegisterResourceError
{
    ResourceAlreadyAdded,
    ResourceDoesNotImplementIResourceInterface
}

internal class ResourceContainer
{
    private readonly Dictionary<Type, object> _resources = new();

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

    public Result<Unit, RegisterResourceError> RegisterResource<T>(T resource) where T : IResource
    {
        if (!_resources.TryAdd(typeof(T), resource))
        {
            return Result.Failure<Unit, RegisterResourceError>(RegisterResourceError.ResourceAlreadyAdded);
        }

        return Result.Success<Unit, RegisterResourceError>(Unit.Value);
    }

    public Result<Unit, RegisterResourceError> RegisterResource(Type resourceType, IResource resource)
    {
        if (!resourceType.IsAssignableTo(typeof(IResource)))
        {
            return Result.Failure<Unit, RegisterResourceError>(RegisterResourceError.ResourceDoesNotImplementIResourceInterface);
        }

        if (!_resources.TryAdd(resourceType, resource))
        {
            return Result.Failure<Unit, RegisterResourceError>(RegisterResourceError.ResourceAlreadyAdded);
        }

        return Result.Success<Unit, RegisterResourceError>(Unit.Value);
    }
}
