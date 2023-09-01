using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Rendering;

namespace MyGame;

public class QuitOnEscapeSystem : ISystem
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
        if (_inputResource.Keyboard.IsKeyDown(MyKey.Escape))
        {
            _window.Close();
        }
    }
}
