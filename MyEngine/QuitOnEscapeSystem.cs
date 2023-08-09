using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class QuitOnEscapeSystem : ISystem
    {
        private readonly MyWindow _window;
        private readonly InputResource _inputResource;

        public QuitOnEscapeSystem(MyWindow window, InputResource inputResource)
        {
            _window = window;
            _inputResource = inputResource;
        }

        public void Run(double deltaTime)
        {
            if (_inputResource.Keyboard.IsKeyDown(Core.Input.MyKey.Escape))
            {
                _window.Close();
            }
        }
    }
}
