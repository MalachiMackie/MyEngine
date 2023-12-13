using System.Numerics;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Input;

public interface IMouse : IResource
{
    public Vector2 Delta { get; }
    public Vector2 Position { get; }
}

public interface IKeyboard : IResource
{
    bool IsKeyDown(MyKey key);
    bool IsKeyPressed(MyKey key);
}
