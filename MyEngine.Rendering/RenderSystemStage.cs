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

public class PreRenderSystemStage : ISystemStage
{
    public static PreRenderSystemStage Instance { get; } = new ();

    private PreRenderSystemStage() { }

    public bool Equals(ISystemStage? other)
    {
        return other is PreRenderSystemStage;
    }
}
