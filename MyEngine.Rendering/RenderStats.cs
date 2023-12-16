using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Rendering;

public class RenderStats : IResource
{
    public uint DrawCalls { get; internal set; }
}
