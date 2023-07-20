using System.Numerics;

namespace MyEngine
{
    internal class Application : IDisposable
    {
        private readonly MyWindow _window;
        private readonly Renderer _renderer;
        private readonly HashSet<EntityId> _entityIds = new ();
        private readonly List<Entity> _entities = new();
        private readonly HashSet<EntityId> _transformComponentEntityIds = new();
        private readonly List<TransformComponent> _transformComponents = new();
        private MyInput _input = null!;

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
            var cameraTransform = new TransformComponent(cameraEntity.Id);
            AddTransformComponent(cameraTransform);
        }

        private void OnLoad()
        {
            _renderer.Load(_window.InnerWindow);
            _input = new MyInput(_window);
            _input.KeyDown += OnKeyDown;
            _input.MouseMove += OnMouseMove;
        }

        private void OnRender(double dt)
        {
            _renderer.Render(_transformComponents[0].Transform);
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

        private Vector2 _lastMousePosition;
        private void OnMouseMove(object? sender, MyInput.MouseMoveEvent e)
        {
            var lookSensitivity = 0.1f;
            var cameraTransform = _transformComponents[0].Transform;

            if (_lastMousePosition != default)
            {
                var yOffset = e.Position.Y - _lastMousePosition.Y;
                var xOffset = e.Position.X - _lastMousePosition.X;

                var q = cameraTransform.rotation;

                var direction = MathHelper.ToEulerAngles(q);

                direction.X += xOffset * lookSensitivity;
                direction.Y -= yOffset * lookSensitivity;

                cameraTransform.rotation = MathHelper.ToQuaternion(direction);
            }

            _lastMousePosition = e.Position;
        }

        private void OnUpdate(double dt)
        {
            var cameraTransform = _transformComponents[0].Transform;
            var cameraDirection = MathHelper.ToEulerAngles(cameraTransform.rotation);

            var cameraFront = Vector3.Normalize(cameraDirection);

            var speed = 5.0f * (float)dt;
            if (_input.IsKeyPressed(MyKey.W))
            {
                cameraTransform.position += (speed * cameraFront);
            }
            if (_input.IsKeyPressed(MyKey.S))
            {
                cameraTransform.position -= (speed * cameraFront);
            }
            if (_input.IsKeyPressed(MyKey.A))
            {
                cameraTransform.position -= speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
            }
            if (_input.IsKeyPressed(MyKey.D))
            {
                cameraTransform.position += speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
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
