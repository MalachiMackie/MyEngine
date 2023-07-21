using Silk.NET.Windowing;
using System.Numerics;

namespace MyEngine.Runtime
{
    internal class MyWindow : IDisposable
    {
        private readonly IWindow _window;

        public MyWindow(string appTitle,
            uint width,
            uint height)
        {
            _window = Window.Create(WindowOptions.Default with
            {
                Title = appTitle,
                Size = new Silk.NET.Maths.Vector2D<int>((int)width, (int)height)
            });

            _window.Resize += size => Resize?.Invoke((Vector2)size);
            _window.Load += () => Load?.Invoke();
            _window.Render += dt => Render?.Invoke(dt);
            _window.Update += dt => Update?.Invoke(dt);
        }

        public void Dispose()
        {
            _window.Dispose();
        }

        public void Run()
        {
            _window.Run();
        }

        public void Close()
        {
            _window.Close();
        }

        public event Action<Vector2>? Resize;
        public event Action? Load;
        public event Action<double>? Render;
        public event Action<double>? Update;

        // todo: better encapsulation
        internal IWindow InnerWindow => _window;
    }
}
