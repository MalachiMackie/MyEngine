using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine
{
    internal class Application : IDisposable
    {
        private readonly MyWindow _window;
        private readonly Renderer _renderer;
        private HashSet<EntityId> _entityIds = new ();
        private List<Entity> _entities = new();
        private HashSet<EntityId> _transformComponentEntityIds = new ();
        private List<TransformComponent> _transformComponents = new();

        private Application(Renderer renderer)
        {
            _renderer = renderer;
            _window = new MyWindow("My OpenGL App",
                800,
                600,
                Render,
                OnUpdate,
                OnLoad);

            var cameraEntity = new Entity();
            AddEntity(cameraEntity);
            var cameraTransform = new TransformComponent(cameraEntity.Id);
            AddTransformComponent(cameraTransform);
        }

        private void OnLoad()
        {
            _renderer.OnLoad(_window.InnerWindow);
        }

        private void Render(double dt)
        {
            _renderer.Render(dt, _transformComponents[0]);
            _skipUpdate = false;
        }

        // todo: result
        private void AddEntity(Entity entity)
        {
            if (_entityIds.Contains(entity.Id))
            {
                throw new InvalidOperationException($"Entity {entity.Id.Value} has already been added");
            }

            _entities.Add(entity);
            _entityIds.Add(entity.Id);
        }

        private void AddTransformComponent(TransformComponent component)
        {
            if (!_entityIds.Contains(component.EntityId))
            {
                throw new InvalidOperationException($"Entity {component.EntityId.Value} has not been added yet");
            }

            if (!TransformComponent.AllowMultiple && _transformComponentEntityIds.Contains(component.EntityId))
            {
                throw new InvalidOperationException($"Transform component has already been added");
            }

            _transformComponentEntityIds.Add(component.EntityId);
            _transformComponents.Add(component);
        }

        public static async Task<Application> CreateAsync()
        {
            var renderer = await Renderer.CreateAsync("My OpenGL App", 800, 600);

            return new Application(renderer);
        }

        public void Run()
        {
            _window.Run();
        }

        private bool _skipUpdate = true;
        private void OnUpdate(double dt)
        {
            if (_skipUpdate) return;

            _renderer.OnUpdate(dt);
        }

        public void Dispose()
        {
            _window.Dispose();
        }
    }
}
