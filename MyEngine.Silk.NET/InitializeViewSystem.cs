using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using Silk.NET.Windowing;

namespace MyEngine.Silk.NET;

internal class InitializeViewSystem : IStartupSystem
{
    private readonly InitialWindowProps _initialWindowProps;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;

    public InitializeViewSystem(InitialWindowProps initialWindowProps, ResourceRegistrationResource resourceRegistrationResource)
    {
        _initialWindowProps = initialWindowProps;
        _resourceRegistrationResource = resourceRegistrationResource;
    }

    public void Run()
    {
        var window = Window.Create(WindowOptions.Default with
        {
            Title = _initialWindowProps.WindowTitle,
            Size = new global::Silk.NET.Maths.Vector2D<int>((int)_initialWindowProps.Width, (int)_initialWindowProps.Height)
        });

        var silkView = new SilkView { View = window };

        _resourceRegistrationResource.AddResource<SilkView, Core.IView>(silkView);
        _resourceRegistrationResource.AddResource<IEcsUpdateRunner>(new OpenGLWindowUpdateRunner(silkView));
    }
}

internal class OpenGLWindowUpdateRunner : IEcsUpdateRunner
{
    private readonly SilkView _view;

    public OpenGLWindowUpdateRunner(SilkView view)
    {
        _view = view;
    }

    public void AddUpdateHandler(Action<double> handler)
    {
        _view.View.Update += handler;
    }

    public void Run()
    {
        _view.View.Run();
    }
}
