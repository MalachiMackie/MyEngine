using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Core;

public class AppBuilder
{
    public AppBuilder AddSystem<T>()
        where T : ISystem
    {
        return this;
    }

    public AppBuilder AddStartupSystem<T>()
        where T : IStartupSystem
    {
        return this;
    }

    public AppBuilder AddResource<T>(T resource)
        where T : IResource
    {
        Resources[typeof(T)] = resource;
        return this;
    }

    public AppBuilder AddPlugin<T>(T plugin)
        where T : IPlugin
    {
        return plugin.Register(this);
    }

    internal Dictionary<Type, IResource> Resources { get; } = new();
}

public interface IPlugin
{
    AppBuilder Register(AppBuilder builder);
}
