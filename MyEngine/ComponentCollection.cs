using System.Collections.Generic;
using System.Collections;

namespace MyEngine
{
    internal class ComponentCollection<TComponent> : IReadOnlyCollection<TComponent>
        where TComponent : IComponent
    {
        private readonly List<TComponent> _components = new();
        private readonly HashSet<EntityId> _entityIds = new();

        public int Count => _components.Count;

        public bool DoesEntityHaveComponent(EntityId entityId) => _entityIds.Contains(entityId);

        public void AddComponent(TComponent component)
        {
            if (!TComponent.AllowMultiple && DoesEntityHaveComponent(component.EntityId))
            {
                throw new InvalidOperationException($"Component has already been added");
            }

            _components.Add(component);
        }

        public IEnumerator<TComponent> GetEnumerator()
        {
            return _components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _components.GetEnumerator();
        }
    }
}
