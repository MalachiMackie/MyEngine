﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Runtime
{
    internal class ComponentCollection
    {
        private readonly Dictionary<Type, Dictionary<EntityId, IComponent>> _components = new();

        public bool DoesEntityHaveComponent<TComponent>(EntityId entityId)
            where TComponent : IComponent
        {
            return _components.TryGetValue(typeof(TComponent), out var components)
                && components.ContainsKey(entityId);
        }

        public void AddComponent(IComponent component)
        {
            var type = component.GetType();
            if (!_components.TryGetValue(type, out var components))
            {
                components = new();
                _components[type] = components;
            }

            if (components.ContainsKey(component.EntityId))
            {
                throw new InvalidOperationException($"Component has already been added");
            }

            components.Add(component.EntityId, component);
        }

        public bool TryGetComponent<TComponent>(EntityId entityId, [NotNullWhen(true)] out TComponent? component)
            where TComponent : class, IComponent
        {
            if (!_components.TryGetValue(typeof(TComponent), out var components))
            {
                component = null;
                return false;
            }

            if (components.TryGetValue(entityId, out var innerComponent))
            {
                if (innerComponent is not TComponent tComponent)
                {
                    throw new UnreachableException();
                }

                component = tComponent;
                return true;
            }

            component = null;
            return false;
        }
    }
}
