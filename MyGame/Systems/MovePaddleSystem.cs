using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;

namespace MyGame.Systems;

public class MovePaddleSystem : ISystem
{
    private readonly IQuery<TransformComponent, PaddleComponent> _query;
    private readonly InputResource _inputResource;

    public MovePaddleSystem(IQuery<TransformComponent, PaddleComponent> query, InputResource inputResource)
    {
        _query = query;
        _inputResource = inputResource;
    }


    public void Run(double deltaTime)
    {
        var paddleComponents = _query.FirstOrDefault();
        if (paddleComponents is null)
        {
            return;
        }

        var (transformComponent, _) = paddleComponents;

        ref var position = ref transformComponent.LocalTransform.position; 
        if (_inputResource.Keyboard.IsKeyDown(MyEngine.Core.Input.MyKey.A))
        {
            position.X -= 1f * (float)deltaTime;
        }
        if (_inputResource.Keyboard.IsKeyDown(MyEngine.Core.Input.MyKey.D))
        {
            position.X += 1f * (float)deltaTime;
        }
    }
}
