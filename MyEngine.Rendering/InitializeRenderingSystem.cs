using MyEngine.Core.Ecs.Systems;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace MyEngine.Rendering;

public class InitializeRenderingSystem : IStartupSystem
{
    private readonly Renderer _renderer;
    private readonly MyWindow _myWindow;

    public InitializeRenderingSystem(
        Renderer renderer,
        MyWindow myWindow)
    {
        _renderer = renderer;
        _myWindow = myWindow;
    }

    public void Run()
    {
        _myWindow.Initialize(
            Load,
            Resize);
    }

    private void Resize(Vector2D<int> size)
    {
        _renderer.Resize(size);
    }

    private void Load(GL openGL)
    {
        var loadResult = _renderer.Load(openGL);

        if (loadResult.TryGetError(out var error))
        {
            error.Match(
                x => Console.WriteLine("Fragment shader failed to compile: {0}", x.CompilationError),
                x => Console.WriteLine("Vertex shader failed to compile: {0}", x.CompilationError),
                x => Console.WriteLine("Shader linking failed: {0}", x.LinkError));
        }
    }
}
