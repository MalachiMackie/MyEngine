using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Core;

public class AppBuilder
{
    internal record StageSystems(HashSet<Type> SystemTypes, uint Order);


    /// <summary>
    /// Collection of resources that have been added to the app builder 
    /// </summary>
    internal Dictionary<Type, IResource> Resources { get; } = new();

    /// <summary>
    /// Collection of startup system types that have been added to the app builder
    /// </summary>
    internal HashSet<Type> StartupSystems { get; } = new();

    /// <summary>
    /// Collection of stages and their systems that have been added to the app builder
    /// </summary>
    internal Dictionary<ISystemStage, StageSystems> StagesAndSystems { get; } = new();

    public AppBuilder AddSystemStage(ISystemStage stage, uint order)
    {
        // validate we haven't already added this stage
        if (StagesAndSystems.ContainsKey(stage))
        {
            Console.WriteLine("Did not add {0} system stage as it has already been added", stage.GetType().Name);
            return this;
        }

        var stageSystems = new HashSet<Type>();

        // find any systems that were added for this stage before
        if (_systemTypesWithoutStages.TryGetValue(stage, out var systemQueue))
        {
            while (systemQueue.TryDequeue(out var systemType))
            {
                stageSystems.Add(systemType);
            }
        }

        StagesAndSystems.Add(stage, new StageSystems(stageSystems, order));
        return this;
    }


    public AppBuilder AddSystem<T>(ISystemStage systemStage)
        where T : ISystem
    {
        var systemType = typeof(T);
    
        // validate we haven't already added this system
        if (!_addedSystems.Add(systemType))
        {
            Console.WriteLine("Did not add {0} system as it has already been added", systemType.Name);
            return this;
        }

        // if the corresponding stage hasn't been added yet, queue it up so when it does get added, we can add it then
        if (!StagesAndSystems.TryGetValue(systemStage, out var value))
        {
            if (!_systemTypesWithoutStages.TryGetValue(systemStage, out var systemQueue))
            {
                systemQueue = new Queue<Type>();
                _systemTypesWithoutStages[systemStage] = systemQueue;
            }
            systemQueue.Enqueue(systemType);
            return this;
        }

        value.SystemTypes.Add(systemType);

        return this;
    }

    public AppBuilder AddStartupSystem<T>()
        where T : IStartupSystem
    {
        StartupSystems.Add(typeof(T));
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

    private readonly Dictionary<ISystemStage, Queue<Type>> _systemTypesWithoutStages = new();
    private readonly HashSet<Type> _addedSystems = new();
}

public interface IPlugin
{
    AppBuilder Register(AppBuilder builder);
}
