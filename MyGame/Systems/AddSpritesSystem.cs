using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyGame.Systems;

public class AddSpritesSystem : ISystem
{
    private readonly InputResource _inputResource;
    private readonly ICommands _entityCommands;

    private readonly IQuery<SpriteComponent, TransformComponent> _query;

    public AddSpritesSystem(InputResource inputResource,
        ICommands entityCommands,
        IQuery<SpriteComponent, TransformComponent> query)
    {
        _inputResource = inputResource;
        _entityCommands = entityCommands;
        _query = query;
    }

    public void Run(double deltaTime)
    {
        return;

        if (!_inputResource.Keyboard.IsKeyPressed(MyKey.Space))
        {
            return;
        }

        // get far left sprite
        var minTransform = _query.Select(x => x.Component2.GlobalTransform)
            .MinBy(x => x.position.X);

        if (minTransform is null)
        {
            // \(o_o)/
            return;
        }

        var newTransform = new MyEngine.Core.Transform
        {
            position = new System.Numerics.Vector3(minTransform.position.X - 1, minTransform.position.Y, minTransform.position.Z),
            rotation = minTransform.rotation,
            scale = minTransform.scale
        };

        _entityCommands.AddEntity(x => x.WithTransform(newTransform)
            .WithSprite()
            .WithoutPhysics());
    }
}
