using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace MyEngine.Silk.NET.Input;

internal class InitializeInputSystem : IStartupSystem
{
    private readonly SilkView _silkView;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;

    public InitializeInputSystem(SilkView silkView, ResourceRegistrationResource resourceRegistrationResource)
    {
        _silkView = silkView;
        _resourceRegistrationResource = resourceRegistrationResource;
    }

    public void Run()
    {
        _silkView.AddLoadAction(OnLoad);
    }

    private void OnLoad()
    {
        var input = _silkView.View.CreateInput();
        var silkKeyboard = input.Keyboards[0];
        var silkMouse = input.Mice[0];
        var mouse = new Mouse(silkMouse);
        var keyboard = new Keyboard(silkKeyboard);
        _resourceRegistrationResource.AddResource<Mouse, MyEngine.Input.IMouse>(mouse);
        _resourceRegistrationResource.AddResource<Keyboard, MyEngine.Input.IKeyboard>(keyboard);
    }
}

internal class SilkView : Core.IView
{
    public required IView View { get; init; }

    public void AddLoadAction(Action loadAction)
    {
        if (View.IsInitialized)
        {
            loadAction();
        }
        else
        {
            View.Load += loadAction;
        }
    }

    public void Exit()
    {
        View.Close();
    }
}
