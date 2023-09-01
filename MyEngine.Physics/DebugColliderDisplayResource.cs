using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Physics;

public class DebugColliderDisplayResource : IResource
{
    public bool DisplayColliders { get; set; }
}
