using System.Collections;
using System.Numerics;

namespace MyEngine
{
    internal class Application : IDisposable
    {
        private readonly MyWindow _window;
        private readonly Renderer _renderer;
        private readonly HashSet<EntityId> _entityIds = new ();
        private readonly List<Entity> _entities = new();
        private readonly ComponentCollection<TransformComponent> _transformComponents = new ();
        private readonly ComponentCollection<CameraComponent> _cameraComponents = new ();
        private MyInput _input = null!;
        private InputResource? _inputResource;
        private readonly CameraMovementSystem _movementSystem = new();

        private Application(Renderer renderer)
        {
            _renderer = renderer;
            _window = new MyWindow("My OpenGL App",
                800,
                600);

            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Update += OnUpdate;
            _window.Resize += OnResize;

            var cameraEntity = new Entity();
            AddEntity(cameraEntity);
            _transformComponents.AddComponent(new TransformComponent(cameraEntity.Id));
            _cameraComponents.AddComponent(new CameraComponent(cameraEntity.Id));
        }

        private void OnLoad()
        {
            _renderer.Load(_window.InnerWindow);
            _input = new MyInput(_window);
            _input.KeyDown += OnKeyDown;
            _inputResource = new InputResource(_input);
        }

        private void OnRender(double dt)
        {
            foreach (var entityId in _entityIds)
            {
                if (_transformComponents.TryGetComponents(entityId, out var transforms))
                {
                    if (_cameraComponents.DoesEntityHaveComponent(entityId))
                    {
                        _renderer.Render(transforms[0].Transform);
                    }
                }
            }
        }

        private void OnResize(Vector2 size)
        {
            _renderer.Resize(size);
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

        public static async Task<Application> CreateAsync()
        {
            var renderer = await Renderer.CreateAsync(800, 600);

            return new Application(renderer);
        }

        public void Run()
        {
            _window.Run();
        }

        private void OnKeyDown(object? sender, MyInput.KeyDownEvent e)
        {
            Console.WriteLine("KeyCode pressed. Key: {0}, KeyCode: {1}", e.Key, e.KeyCode);
            if (e.Key == MyKey.Escape)
            {
                Close();
            }
        }

        private Vector2 _previousMousePosition;
        private void OnUpdate(double dt)
        {
            if (_inputResource is not null)
            {
                var position = _inputResource.Input.Mouse.Position;
                if (_previousMousePosition != default)
                {
                    _inputResource.MouseDelta = position - _previousMousePosition;
                }

                _previousMousePosition = position;
            }

            foreach (var entityId in _entityIds)
            {
                if (_transformComponents.TryGetComponents(entityId, out var transforms))
                {
                    if (_cameraComponents.TryGetComponents(entityId, out var cameraComponents))
                    {
                        if (_inputResource is not null)
                        {
                            _movementSystem.Run(dt, transforms[0], cameraComponents[0], _inputResource);
                        }
                    }
                }
            }
        }

        private void Close()
        {
            _window.Close();
        }

        public void Dispose()
        {
            _window.Dispose();
        }
    }
}
