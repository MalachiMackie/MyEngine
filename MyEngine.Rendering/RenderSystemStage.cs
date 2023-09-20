using MyEngine.Core;

namespace MyEngine.Rendering;

public class RenderSystemStage : ISystemStage
{
    public static RenderSystemStage Instance { get; } = new RenderSystemStage();

    private RenderSystemStage() { }

    public bool Equals(ISystemStage? other)
    {
        return other is RenderSystemStage;
    }
}
