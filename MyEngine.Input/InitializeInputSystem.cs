using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using Silk.NET.Input;

namespace MyEngine.Input;

public class InitializeInputSystem : IStartupSystem
{
    private readonly MyInput _myInput;
    private readonly MyWindow _myWindow;

    public InitializeInputSystem(MyWindow myWindow, MyInput myInput)
    {
        _myWindow = myWindow;
        _myInput = myInput;
    }

    public void Run()
    {
        _myWindow.AddLoadAction(() =>
        {
            _myInput.Initialize(_myWindow.InnerWindow!.CreateInput());
        });
    }
}
