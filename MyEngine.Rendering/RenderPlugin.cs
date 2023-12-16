using System.Runtime.CompilerServices;
using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Rendering.RenderSystems;

[assembly: InternalsVisibleTo("MyEngine.Input")]
[assembly: InternalsVisibleTo("MyEngine.ImGui")]
[assembly: InternalsVisibleTo("MyEngine.Runtime")]
[assembly: InternalsVisibleTo("MyEngine.Rendering.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace MyEngine.Rendering;

public record InitialWindowProps(string WindowTitle, uint Width, uint Height) : IResource;

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
            .AddSystemStage(PreRenderSystemStage.Instance, 6)
            .AddSystemStage(RenderSystemStage.Instance, 5)
            .AddResource<ILineRenderResource>(new LineRenderResource())
            .AddResource<IRenderCommandQueue>(new RenderCommandQueue())
            .AddResource(new RenderStats())
            .AddSystem<RenderSystem>(RenderSystemStage.Instance)
            .AddSystem<LineRenderSystem>(PreRenderSystemStage.Instance)
            .AddSystem<SpriteRenderSystem>(PreRenderSystemStage.Instance);
    }
}
