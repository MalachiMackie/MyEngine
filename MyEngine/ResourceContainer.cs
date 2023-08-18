using MyEngine.Core.Ecs.Resources;
using System.Diagnostics.CodeAnalysis;

namespace MyEngine.Runtime;

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

    public void RegisterResource<T>(T resource) where T : IResource
    {
        if (!_resources.TryAdd(typeof(T), resource))
        {
            throw new InvalidOperationException("Resource has already been added");
        }
    }

    public void RegisterResource(Type resourceType, IResource resource)
    {
        if (!resourceType.IsAssignableTo(typeof(IResource)))
        {
            throw new InvalidOperationException("Resource must implement IResource");
        }

        if (!_resources.TryAdd(resourceType, resource))
        {
            throw new InvalidOperationException("Resource has already been added");
        }
    }
}
