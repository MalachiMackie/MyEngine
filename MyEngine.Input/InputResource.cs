using System.Numerics;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Input;

public class InputResource : IResource
{
    internal InputResource()
    {
    }

    public MyMouse Mouse { get; } = new();

    public MyKeyboard Keyboard { get; } = new();

    public Vector2 MouseDelta { get; internal set; }
}
