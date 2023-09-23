using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using Silk.NET.OpenGL;

namespace MyEngine.Rendering;

public record InitialWindowProps(string Title, uint Width, uint Height) : IResource;

public class InitializeRenderingSystem : IStartupSystem
{
    private readonly ResourceRegistrationResource _resourceRegistrationResource;
    private readonly InitialWindowProps _initialWindowProps;

    public InitializeRenderingSystem(
        ResourceRegistrationResource resourceRegistrationResource,
        InitialWindowProps initialWindowProps
        )
    {
        _resourceRegistrationResource = resourceRegistrationResource;
        _initialWindowProps = initialWindowProps;
    }

    public void Run()
    {
        var window = MyWindow.Create(_initialWindowProps.Title,
            _initialWindowProps.Width,
            _initialWindowProps.Height,
            Load);
        _resourceRegistrationResource.AddResource(window);
    }

    private void Load(GL openGL, MyWindow myWindow)
    {
        var rendererResult = Renderer.Create(openGL);

        if (rendererResult.TryGetError(out var error))
        {
            error.Match(
                x => Console.WriteLine("Fragment shader failed to compile: {0}", x.CompilationError),
                x => Console.WriteLine("Vertex shader failed to compile: {0}", x.CompilationError),
                x => Console.WriteLine("Shader linking failed: {0}", x.LinkError));
        }

        var renderer = rendererResult.Unwrap();

        myWindow.Resize += renderer.Resize;

        // todo: do this in the engine step, rather than on load
        _resourceRegistrationResource.AddResource(renderer);
    }
}
