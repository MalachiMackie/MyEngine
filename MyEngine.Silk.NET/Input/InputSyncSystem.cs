using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Silk.NET.Input;

public class InputSyncSystem : ISystem
{
    private readonly Keyboard _keyboard;
    private readonly Mouse _mouse;

    public InputSyncSystem(Keyboard keyboard, Mouse mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;
    }


    public void Run(double _)
    {
        _mouse.Update();
        _keyboard.Update();
    }
}
