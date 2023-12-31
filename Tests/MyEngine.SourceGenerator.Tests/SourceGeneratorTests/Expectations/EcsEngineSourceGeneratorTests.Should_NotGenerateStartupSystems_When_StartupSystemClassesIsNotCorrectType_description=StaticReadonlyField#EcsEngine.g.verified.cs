﻿//HintName: EcsEngine.g.cs
// <auto-generated />
#nullable enable
#pragma warning disable CS0162 // Unreachable code detected

namespace MyEngine.Runtime
{
    internal partial class EcsEngine
    {
        private static partial global::MyEngine.Core.IAppEntrypoint GetAppEntrypoint() => new global::MyAppEntrypoint();

        private partial void AddStartupSystemInstantiations()
        {
            
        }


        private partial void AddSystemInstantiations()
        {
            _systemInstantiations.Add(typeof(global::MyNamespace.MySystem), () =>
            {
                
                if (_resourceContainer.TryGetResource<global::MyNamespace.MyResource>(out var resource0))
                {
                    return new global::MyNamespace.MySystem(
                        resource0);
                }
                return null;
            });
            _systemInstantiations.Add(typeof(global::MyEngine.Core.TransformSyncSystem), () =>
            {
                global::MyEngine.Core.Ecs.Components.EntityComponents<global::MyEngine.Core.Ecs.Components.TransformComponent,
                                                                      global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyEngine.Core.Ecs.Components.ParentComponent>,
                                                                      global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyEngine.Core.Ecs.Components.ChildrenComponent>>? GetQuery1Components(global::MyEngine.Core.Ecs.EntityId entityId)
                {
                    if (_components.TryGetComponent<global::MyEngine.Core.Ecs.Components.TransformComponent>(entityId, out var component1))
                    {
                        var component2 = _components.GetOptionalComponent<global::MyEngine.Core.Ecs.Components.ParentComponent>(entityId);
                        var component3 = _components.GetOptionalComponent<global::MyEngine.Core.Ecs.Components.ChildrenComponent>(entityId);
                        return new global::MyEngine.Core.Ecs.Components.EntityComponents<global::MyEngine.Core.Ecs.Components.TransformComponent,
                                                                                         global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyEngine.Core.Ecs.Components.ParentComponent>,
                                                                                         global::MyEngine.Core.Ecs.Components.OptionalComponent<global::MyEngine.Core.Ecs.Components.ChildrenComponent>>(entityId)
                        {
                            Component1 = component1,
                            Component2 = component2,
                            Component3 = component3
                        };
                    }
                    return null;
                }
                if (true)
                {
                    return new global::MyEngine.Core.TransformSyncSystem(
                        global::MyEngine.Runtime.Query.Create(_components, _entities, GetQuery1Components));
                }
                return null;
            });
        }

        private static partial IReadOnlyCollection<Type> GetAllStartupSystemTypes() =>
            Array.Empty<Type>();

        private static partial IReadOnlyCollection<Type> GetAllSystemTypes() =>
            new Type[]
            {
                typeof(global::MyNamespace.MySystem),
                typeof(global::MyEngine.Core.TransformSyncSystem)
            };

        private static partial Dictionary<System.Type, System.Type[]> GetUninstantiatedStartupSystems() =>
            new ();

        private static partial Dictionary<System.Type, System.Type[]> GetUninstantiatedSystems() =>
            new ()
            {
                { typeof(global::MyNamespace.MySystem), new Type[] { typeof(global::MyNamespace.MyResource) } },
                { typeof(global::MyEngine.Core.TransformSyncSystem), new Type[] {  } }
            };
    }
}

#pragma warning restore CS0162 // Unreachable code detected
#nullable restore
