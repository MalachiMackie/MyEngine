using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;

namespace MyGame.Systems
{
    public class ToggleSpriteSystem : ISystem
    {
        private IEnumerable<EntityComponents<PlayerComponent>> _playerQuery;
        // todo: optional component queries
        private IEnumerable<EntityComponents<PlayerComponent, SpriteComponent>> _playerWithSpriteComponentQuery;
        private ComponentContainerResource _componentsResource;
        private InputResource _inputResource;

        public ToggleSpriteSystem(
            IEnumerable<EntityComponents<PlayerComponent>> playerQuery,
            IEnumerable<EntityComponents<PlayerComponent, SpriteComponent>> playerWithSpriteComponentQuery,
            ComponentContainerResource componentsResource,
            InputResource inputResource)
        {
            _playerQuery = playerQuery;
            _playerWithSpriteComponentQuery = playerWithSpriteComponentQuery;
            _componentsResource = componentsResource;
            _inputResource = inputResource;
        }

        public void Run(double deltaTime)
        {
            if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.Space))
            {
                var player = _playerQuery.FirstOrDefault();
                if (player is null)
                {
                    return;
                }

                var hasSprite = _playerWithSpriteComponentQuery.Any();

                if (hasSprite)
                {
                    _componentsResource.RemoveComponent<SpriteComponent>(player.EntityId);
                }
                else
                {
                    _componentsResource.AddComponent(player.EntityId, new SpriteComponent());
                }
            }
        }
    }
}
