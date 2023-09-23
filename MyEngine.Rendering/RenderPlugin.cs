using System.Runtime.CompilerServices;
using MyEngine.Core;

[assembly: InternalsVisibleTo("MyEngine.Input")]
[assembly: InternalsVisibleTo("MyEngine.ImGui")]

namespace MyEngine.Rendering;

public class RenderPlugin : IPlugin
{
    private readonly string _windowTitle;
    private readonly uint _width;
    private readonly uint _height;

    public RenderPlugin(string windowTitle, uint width = 800, uint height = 600)
    {
        _windowTitle = windowTitle;
        _width = width;
        _height = height;
    }

    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddResource(new InitialWindowProps(_windowTitle, _width, _height))
            .AddStartupSystem<InitializeRenderingSystem>()
            .AddSystemStage(RenderSystemStage.Instance, 5)
            .AddResource<ILineRenderResource>(new LineRenderResource())
            .AddSystem<RenderSystem>(RenderSystemStage.Instance)
            ;
    }
}
