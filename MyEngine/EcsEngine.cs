using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Rendering;
using System.Diagnostics;

namespace MyEngine.Runtime;

internal partial class EcsEngine
{
    private readonly AppBuilder _appBuilder;

    public EcsEngine(AppBuilder appBuilder)
    {
        _appBuilder = appBuilder;
    }

    public void Run()
    {
        AddSystemInstantiations();

        CreateSystemsWithoutResourceDependencies();
        RegisterAppResources();
        RunStartupSystems();

        StartUpdate();
    }

    private readonly Dictionary<Type, Action> _systemInstantiations = new();

    private partial void AddSystemInstantiations();
    private partial void RunStartupSystems();

    private void RegisterAppResources()
    {
        // todo: move these to a plugin
        RegisterResource<IHierarchyCommands>(new HierarchyCommands(_components));
        RegisterResource(new ResourceRegistrationResource());
        RegisterResource<ICommands>(new Commands(_components, _entities));

        foreach (var (resourceType, resource) in _appBuilder.Resources)
        {
            RegisterResource(resourceType, resource);
        }
    }

    private void CreateSystemsWithoutResourceDependencies()
    {
        foreach (var (systemType, _) in _uninstantiatedSystems
            .Where(x => x.Value.Length == 0)
            .ToArray())
        {
            // todo: currently each system instantiation will remove itself from `_uninstantiatedSystems`. I want to find a clearer way to do that.
            // it feels like a weird side effect rather than a clear pattern
            _systemInstantiations[systemType].Invoke();
        }
    }

    private void StartUpdate()
    {
        _resourceContainer.TryGetResource<MyWindow>(out var myWindow);
        myWindow!.Update += Update;
        myWindow!.Run();
    }

    private void AddQueuedResources()
    {
        Debug.Assert(_resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resourceRegistration));
        while (resourceRegistration.Registrations.TryDequeue(out var resource))
        {
            if (_resourceContainer.RegisterResource(resource.Key, resource.Value).TryGetError(out var registerResourceError))
            {
                Console.WriteLine("Failed to register resource: {0}", registerResourceError);
                continue;
            }

            foreach (var (systemType, resourceTypes) in _uninstantiatedSystems)
            {
                if (resourceTypes.Contains(resource.Key))
                {
                    _systemInstantiations[systemType].Invoke();
                }
            }
        }
    }

    private void RegisterResource(Type resourceType, IResource resource)
    {
        if (_resourceContainer.RegisterResource(resourceType, resource).TryGetError(out var error))
        {
            Console.WriteLine("Failed to register resource: {0}", error);
            return;
        }

        foreach (var (systemType, resourceTypes) in _uninstantiatedSystems)
        {
            if (resourceTypes.Contains(resourceType))
            {
                _systemInstantiations[systemType].Invoke();
            }
        }
    }

    public void RegisterResource<T>(T resource) where T : IResource
    {
        RegisterResource(typeof(T), resource);
    }
}
