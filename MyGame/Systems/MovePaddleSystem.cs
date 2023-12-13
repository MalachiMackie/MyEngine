using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;
using MyGame.Components;

namespace MyGame.Systems;

public class MovePaddleSystem : ISystem
{
    private readonly IQuery<TransformComponent, PaddleComponent> _query;
    private readonly IKeyboard _keyboard;

    public MovePaddleSystem(IQuery<TransformComponent, PaddleComponent> query, IKeyboard keyboard)
    {
        _query = query;
        _keyboard = keyboard;
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
        if (_keyboard.IsKeyDown(MyKey.A))
        {
            position.X -= 1.5f * (float)deltaTime;
        }
        if (_keyboard.IsKeyDown(MyKey.D))
        {
            position.X += 1.5f * (float)deltaTime;
        }
    }
}
