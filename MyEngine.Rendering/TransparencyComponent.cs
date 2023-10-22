
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Rendering;

public class TransparencyComponent : IComponent
{
    public required float Transparency { get; set; }
}
