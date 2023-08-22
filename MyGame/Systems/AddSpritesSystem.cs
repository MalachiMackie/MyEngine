using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyGame.Systems;

public class AddSpritesSystem : ISystem
{
    private readonly InputResource _inputResource;
    private readonly IEntityCommands _entityCommands;
    private readonly ComponentContainerResource _componentContainerResource;

    private readonly IQuery<SpriteComponent, TransformComponent> _query;

    public AddSpritesSystem(InputResource inputResource,
        IEntityCommands entityCommands,
        ComponentContainerResource componentContainerResource,
        IQuery<SpriteComponent, TransformComponent> query)
    {
        _inputResource = inputResource;
        _entityCommands = entityCommands;
        _componentContainerResource = componentContainerResource;
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
        var minTransform = _query.Select(x => x.Component2.Transform)
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

        var newEntity = _entityCommands.AddEntity(newTransform);
        _componentContainerResource.AddComponent(newEntity, new SpriteComponent());
    }
}
