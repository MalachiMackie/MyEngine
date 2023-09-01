using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyEngine.Physics;

namespace MyGame.Systems;
public class ToggleColliderDebugDisplaySystem : ISystem
{
    private readonly InputResource _inputResource;
    private readonly DebugColliderDisplayResource _debugColliderDisplayResource;

    public ToggleColliderDebugDisplaySystem(DebugColliderDisplayResource debugColliderDisplayResource, InputResource inputResource)
    {
        _debugColliderDisplayResource = debugColliderDisplayResource;
        _inputResource = inputResource;
    }

    public void Run(double deltaTime)
    {
        if (_inputResource.Keyboard.IsKeyPressed(MyKey.F1))
        {
            _debugColliderDisplayResource.DisplayColliders = !_debugColliderDisplayResource.DisplayColliders;
        }
    }
}
