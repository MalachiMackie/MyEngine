using System.Numerics;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Input;

namespace MyEngine.Runtime
{
    internal class Application : IDisposable
    {
        private readonly MyWindow _window;
        private readonly Renderer _renderer;

        private readonly EcsEngine _systemRunner = new();

        private Application(Renderer renderer)
        {
            _renderer = renderer;
            _systemRunner.RegisterResource(_renderer);
            _window = new MyWindow("My OpenGL App",
                800,
                600);

            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Update += OnUpdate;
            _window.Resize += OnResize;

            _systemRunner.RegisterResource(_window);
        }

        private void OnLoad()
        {
            _renderer.Load(_window.InnerWindow);
            var input = new MyInput(_window);
            input.KeyDown += OnKeyDown;

            _systemRunner.RegisterResource(new InputResource(new MyKeyboard(), new MyMouse()));
            _systemRunner.RegisterResource(input);
            _systemRunner.Startup();
        }

        private void OnRender(double dt)
        {
            _systemRunner.Render(dt);
        }

        private void OnResize(Vector2 size)
        {
            _renderer.Resize(size);
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
        }

        private void OnUpdate(double dt)
        {
            _systemRunner.Update(dt);
        }

        public void Dispose()
        {
            _window.Dispose();
        }
    }
}
