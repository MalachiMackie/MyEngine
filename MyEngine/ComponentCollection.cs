using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MyEngine
{
    internal class ComponentCollection<TComponent>
        where TComponent : IComponent
    {
        private readonly Dictionary<EntityId, List<TComponent>> _components = new();

        public bool DoesEntityHaveComponent(EntityId entityId) => _components.ContainsKey(entityId);

        public void AddComponent(TComponent component)
        {
            if (TComponent.AllowMultiple)
            {
                if (!_components.TryGetValue(component.EntityId, out var components))
                {
                    components = new List<TComponent>();
                    _components[component.EntityId] = components;
                }
                components.Add(component);
                return;
            }

            if (_components.ContainsKey(component.EntityId))
            {
                throw new InvalidOperationException($"Component has already been added");
            }

            _components.Add(component.EntityId, new List<TComponent>{component});
        }

        public bool TryGetComponents(EntityId entityId, [NotNullWhen(true)] out IReadOnlyList<TComponent>? components)
        {
            if (_components.TryGetValue(entityId, out var innerComponents))
            {
                components = innerComponents;
                return true;
            }

            components = null;
            return false;
        }
    }
}
