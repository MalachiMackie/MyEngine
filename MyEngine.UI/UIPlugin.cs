using MyEngine.Core;
using MyEngine.Rendering;
using MyEngine.Rendering.RenderSystems;

namespace MyEngine.UI;
public class UIPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddSystem<UITextRenderSystem>(PreRenderSystemStage.Instance);
    }
}
