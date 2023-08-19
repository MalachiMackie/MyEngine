﻿namespace MyEngine.Core.Ecs.Resources;

public class ResourceRegistrationResource : IResource
{
    internal Queue<KeyValuePair<Type, IResource>> Registrations { get; } = new(); 

    public void AddResource<T>(T resource)
        where T : IResource
    {
        Registrations.Enqueue(KeyValuePair.Create<Type, IResource>(typeof(T), resource));
    }
}