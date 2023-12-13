using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Physics;

namespace MyGame.Systems;
public class ToggleColliderDebugDisplaySystem : ISystem
{
    private readonly DebugColliderDisplayResource _debugColliderDisplayResource;
    private readonly IKeyboard _keyboard;

    public ToggleColliderDebugDisplaySystem(DebugColliderDisplayResource debugColliderDisplayResource, IKeyboard keyboard)
    {
        _debugColliderDisplayResource = debugColliderDisplayResource;
        _keyboard = keyboard;
    }

    public void Run(double deltaTime)
    {
        if (_keyboard.IsKeyPressed(MyKey.F1))
        {
            _debugColliderDisplayResource.DisplayColliders = !_debugColliderDisplayResource.DisplayColliders;
        }
    }
}
