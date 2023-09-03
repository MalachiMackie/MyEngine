using MyEngine.Core;

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
            .AddSystemStage(RenderSystemStage.Instance, 4)
            .AddResource(new Renderer(_width, _height))
            .AddResource(new MyWindow(_windowTitle, _width, _height))
            .AddResource<ILineRenderResource>(new LineRenderResource())
            .AddSystem<RenderSystem>(RenderSystemStage.Instance)
            .AddStartupSystem<InitializeRenderingSystem>();
    }
}
