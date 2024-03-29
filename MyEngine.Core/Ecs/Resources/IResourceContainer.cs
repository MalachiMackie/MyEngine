using System.Diagnostics.CodeAnalysis;
using MyEngine.Utils;

namespace MyEngine.Core.Ecs.Resources;

public interface IResourceContainer
{
    public bool TryGetResource<T>([NotNullWhen(true)] out T? resource) where T : class, IResource;

    public Result<Unit> RegisterResource<T>(T resource) where T : IResource;

    public Result<Unit> RegisterResource(Type resourceType, IResource resource);
}
