using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using System.Diagnostics;

namespace MyEngine.Runtime;

// todo: WithoutComponent<T>

internal partial class EcsEngine
{
    private sealed record StageSystems(ISystemStage Stage, List<ISystem> Systems);

    private readonly AppBuilder _appBuilder = new();
    private readonly ResourceContainer _resourceContainer = new();
    private readonly HashSet<Core.Ecs.EntityId> _entities = new();
    private readonly ComponentCollection _components = new();
    private bool _inStartupStage = true;
    private readonly Dictionary<Type, Func<IStartupSystem?>> _startupSystemInstantiations = new();
    private readonly Dictionary<Type, Func<ISystem?>> _systemInstantiations = new();
    private readonly Queue<IStartupSystem> _startupSystems = new();
    private StageSystems[] _systems = null!;

    private readonly IReadOnlyCollection<Type> _allStartupSystemTypes = GetAllStartupSystemTypes();
    private readonly IReadOnlyCollection<Type> _allSystemTypes = GetAllSystemTypes();
    private readonly Dictionary<Type, Type[]> _uninstantiatedStartupSystems = GetUninstantiatedStartupSystems();
    private readonly Dictionary<Type, Type[]> _uninstantiatedSystems = GetUninstantiatedSystems();

    public EcsEngine()
    {
        var appEntrypoint = GetAppEntrypoint();
        appEntrypoint.BuildApp(_appBuilder);
    }

    private void Setup()
    {
        AddSystemInstantiations();
        AddStartupSystemInstantiations();

        // source generator adds all system types found, but maybe we don't want all the systems.
        // remove any we dont want
        var appBuilderSystemTypes = _appBuilder.StagesAndSystems.SelectMany(x => x.Value.SystemTypes);
        _systems = _appBuilder.StagesAndSystems
            .OrderBy(x => x.Value.Order)
            .Select(x => new StageSystems(x.Key, new List<ISystem>()))
            .ToArray();

        foreach (var systemType in _allSystemTypes.Except(appBuilderSystemTypes))
        {
            Console.WriteLine("Removing {0} system as it wasn't added to the app builder", systemType.Name);
            _uninstantiatedSystems.Remove(systemType);
            _systemInstantiations.Remove(systemType);
        }
        foreach (var systemType in _allStartupSystemTypes.Except(_appBuilder.StartupSystems))
        {
            _uninstantiatedStartupSystems.Remove(systemType);
            _startupSystemInstantiations.Remove(systemType);
        }

        CreateSystemsWithoutResourceDependencies();
        RegisterAppResources();
    }

    public void Update(double dt)
    {
        foreach (var (stage, systems) in _systems)
        {
            foreach (var system in systems)
            {
                system.Run(dt);
            }
        }

        AddQueuedResources();
    }

    public void Run()
    {
        Setup();
        RunStartupSystems();
        if (_startupSystems.Count > 0)
        {
            Console.WriteLine("Wasn't able to run {0} startup systems", _startupSystems.Count);
        }
        _inStartupStage = false;

        StartUpdate();
    }

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
            .Where(x => x.Value.Length == 0))
        {
            TryInstantiateSystem(systemType);
        }

        foreach (var (startupSystemType, _) in _uninstantiatedStartupSystems
            .Where(x => x.Value.Length == 0))
        {
            TryInstantiateStartupSystem(startupSystemType);
        }
    }

    private void StartUpdate()
    {
        _resourceContainer.TryGetResource<MyWindow>(out var myWindow);
        myWindow!.Update += Update;
        myWindow!.Run();
    }

    private void RunStartupSystems()
    {
        while (_startupSystems.TryDequeue(out var startupSystem))
        {
            startupSystem.Run();
            AddQueuedResources();
        }
    }

    private void AddQueuedResources()
    {
        Debug.Assert(_resourceContainer.TryGetResource<ResourceRegistrationResource>(out var resourceRegistration));
        while (resourceRegistration.Registrations.TryDequeue(out var resource))
        {
            RegisterResource(resource.Key, resource.Value);
        }
    }

    private void TryInstantiateSystem(Type systemType)
    {
        var system = _systemInstantiations[systemType].Invoke();
        if (system is not null)
        {
            var stage = _appBuilder.StagesAndSystems.First(x => x.Value.SystemTypes.Contains(systemType)).Key;
            var systems = _systems.First(x => x.Stage == stage).Systems;

            systems.Add(system);
            
            _uninstantiatedSystems.Remove(systemType);
        }
    }

    private void TryInstantiateStartupSystem(Type startupSystemType)
    {
        var startupSystem = _startupSystemInstantiations[startupSystemType].Invoke();
        if (startupSystem is not null)
        {
            _startupSystems.Enqueue(startupSystem);
            _uninstantiatedStartupSystems.Remove(startupSystemType);
        }
    }

    private void RegisterResource(Type resourceType, IResource resource)
    {
        // validate we haven't already added this resource
        if (_resourceContainer.RegisterResource(resourceType, resource).TryGetError(out var error))
        {
            Console.WriteLine("Failed to register resource: {0}", error);
            return;
        }

        // try and instantiate any systems that have yet to be instantiated and that have this resource as a dependency
        foreach (var (systemType, _) in _uninstantiatedSystems
            .Where(x => x.Value.Contains(resourceType)))
        {
            TryInstantiateSystem(systemType);
        }

        if (!_inStartupStage)
        {
            return;
        }

        // if we're still in startup stage, try and instantiate any startup systems, so that can be run
        foreach (var (startupSystemType, _) in _uninstantiatedStartupSystems
            .Where(x => x.Value.Contains(resourceType)))
        {
            TryInstantiateStartupSystem(startupSystemType);
        }
    }

    public void RegisterResource<T>(T resource) where T : IResource
    {
        RegisterResource(typeof(T), resource);
    }

    private partial void AddSystemInstantiations();
    private partial void AddStartupSystemInstantiations();
    private static partial IAppEntrypoint GetAppEntrypoint();
    private static partial IReadOnlyCollection<Type> GetAllStartupSystemTypes();
    private static partial IReadOnlyCollection<Type> GetAllSystemTypes();
    private static partial Dictionary<Type, Type[]> GetUninstantiatedStartupSystems();
    private static partial Dictionary<Type, Type[]> GetUninstantiatedSystems();
}
