using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine
{
    internal class MyWindow : IDisposable
    {
        private readonly IWindow _window;

        public MyWindow(string appTitle,
            uint width,
            uint height,
            Action<double> onRender,
            Action<double> onUpdate,
            Action onLoad)
        {
            _window = Window.Create(WindowOptions.Default with
            {
                Title = appTitle,
                Size = new Silk.NET.Maths.Vector2D<int>((int)width, (int)height)
            });

            _window.Render += onRender;
            _window.Update += onUpdate;
            _window.Load += onLoad;
        }

        public void Dispose()
        {
            _window.Dispose();
        }

        public void Run()
        {
            _window.Run();
        }

        // todo: better encapsulation
        public IWindow InnerWindow => _window;
    }
}
