using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

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
        if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.F1))
        {
            _debugColliderDisplayResource.DisplayColliders = !_debugColliderDisplayResource.DisplayColliders;
        }
    }
}
