using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Core.Ecs;
public interface IEcsEngineGlue
{
    public void Init(ISet<EntityId> entities, ComponentCollection components, IResourceContainer resourceContainer);

    Dictionary<Type, Func<ISystem?>> GetSystemInstantiations();
    Dictionary<Type, Func<IStartupSystem?>> GetStartupSystemInstantiations();
    IAppEntrypoint GetAppEntrypoint();
    IReadOnlyCollection<Type> GetAllStartupSystemTypes();
    IReadOnlyCollection<Type> GetAllSystemTypes();
    Dictionary<Type, Type[]> GetUninstantiatedStartupSystems();
    Dictionary<Type, Type[]> GetUninstantiatedSystems();
}
