using System.Numerics;
using MyEngine.Input;
using ISilkMouse = Silk.NET.Input.IMouse;

namespace MyEngine.Silk.NET.Input;

public class Mouse : IMouse
{
    private ISilkMouse _silkMouse;

    public Mouse(ISilkMouse silkMouse)
    {
        _silkMouse = silkMouse;
    }

    public Vector2 Delta { get; private set; }
    public Vector2 Position { get; private set; }

    public void Update()
    {
        var newPosition = _silkMouse.Position;
        Delta = newPosition - Position;
        Position = newPosition;
    }
}
