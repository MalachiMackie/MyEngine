namespace MyEngine.Core.Ecs.Resources;

// todo: should be resourceCommands
public class ResourceRegistrationResource : IResource
{
    internal Queue<KeyValuePair<Type, IResource>> Registrations { get; } = new(); 

    public void AddResource<T>(T resource)
        where T : IResource
    {
        Registrations.Enqueue(KeyValuePair.Create<Type, IResource>(typeof(T), resource));
    }

    /// <summary>
    /// Add a resource under two different types
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="resource"></param>
    public void AddResource<T1, T2>(T1 resource)
        where T1 : IResource, T2
    {
        Registrations.Enqueue(KeyValuePair.Create<Type, IResource>(typeof(T1), resource));
        Registrations.Enqueue(KeyValuePair.Create<Type, IResource>(typeof(T2), resource));
    }
}
