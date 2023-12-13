using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;

namespace MyGame;

public class QuitOnEscapeSystem : ISystem
{
    private readonly IKeyboard _keyboard;
    private readonly IView _view;

    public QuitOnEscapeSystem(IKeyboard keyboard, IView view)
    {
        _keyboard = keyboard;
        _view = view;
    }

    public void Run(double deltaTime)
    {
        if (_keyboard.IsKeyPressed(MyKey.Escape))
        {
            _view.Exit();
        }
    }
}
