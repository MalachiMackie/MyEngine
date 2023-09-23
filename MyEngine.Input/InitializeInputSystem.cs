using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using Silk.NET.Input;

namespace MyEngine.Input;

public class InitializeInputSystem : IStartupSystem
{
    private readonly MyWindow _myWindow;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;

    public InitializeInputSystem(MyWindow myWindow, ResourceRegistrationResource resourceRegistrationResource)
    {
        _myWindow = myWindow;
        _resourceRegistrationResource = resourceRegistrationResource;
    }

    public void Run()
    {
        _myWindow.AddLoadAction((window) =>
        {
            _resourceRegistrationResource.AddResource(
                new MyInput(window.GlWindow.CreateInput()));
        });
    }
}
