using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
