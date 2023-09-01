using System.Numerics;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Rendering;

public class Camera2DComponent : IComponent
{
    public Vector2 Size { get; set; }

    public Camera2DComponent(Vector2 size)
    {
        Size = size;
    }
}
