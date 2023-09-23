using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Rendering;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace MyEngine.ImGui;

public class ImGuiResource : IResource
{
    internal ImGuiController Controller { get; }

    internal ImGuiResource(ImGuiController controller)
    {
        Controller = controller;
    }
}

public class InitailizeImGuiSystem : ISystem
{
    private readonly MyWindow _window;
    private readonly MyInput _input;
    private readonly Renderer _renderer;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;

    public InitailizeImGuiSystem(
        MyWindow window,
        MyInput input,
        Renderer renderer,
        ResourceRegistrationResource resourceRegistrationResource)
    {
        _window = window;
        _input = input;
        _renderer = renderer;
        _resourceRegistrationResource = resourceRegistrationResource;
    }

    private bool _initialized;

    public void Run(double dt)
    {
        if (_initialized)
        {
            return;
        }

        var controller = new ImGuiController(
            _renderer.OpenGL,
            _window.GlWindow,
            _input.GlInputContext);
        _resourceRegistrationResource.AddResource(new ImGuiResource(controller));
        _initialized = true;
    }
}
