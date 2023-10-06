using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Rendering;
using MyEngine.UI;

namespace MyEngine.Rendering.RenderSystems;

public class UIRenderSystem : ISystem
{
    private readonly RenderCommandQueue _renderCommandQueue;
    private readonly IQuery<UICanvasComponent, ChildrenComponent> _canvasQuery;
    private readonly IQuery<UITextComponent, UITransformComponent> _textQuery;
    private readonly IQuery<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>> _boxQuery;

    public UIRenderSystem(IQuery<UITextComponent, UITransformComponent> textQuery,
        IQuery<UICanvasComponent, ChildrenComponent> canvasQuery,
        RenderCommandQueue renderCommandQueue,
        IQuery<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>> boxQuery)
    {
        _textQuery = textQuery;
        _canvasQuery = canvasQuery;
        _renderCommandQueue = renderCommandQueue;
        _boxQuery = boxQuery;
    }

    public void Run(double deltaTime)
    {
        foreach (var (_, childrenComponent) in _canvasQuery)
        {
            foreach (var childEntity in childrenComponent.Children)
            {
                RenderEntityAndChildren(childEntity, new Vector2());
            }
        }
    }

    private void RenderEntityAndChildren(EntityId entity, Vector2 position)
    {
        var textComponents = _textQuery.TryGetForEntity(entity);
        if (textComponents is not null)
        {
            var (textComponent, uiTransformComponent) = textComponents;
            _renderCommandQueue.Enqueue(new RenderScreenSpaceTextCommand(
                textComponent.Font.Texture,
                textComponent.Font.CharSprites,
                textComponent.Text,
                textComponent.Transparency ?? 1f,
                position + uiTransformComponent.Position));
        }
        var boxComponents = _boxQuery.TryGetForEntity(entity);
        if (boxComponents is not null)
        {
            var (boxComponent, uiTransformComponent, maybeChildrenComponent) = boxComponents;
            _renderCommandQueue.Enqueue(new RenderScreenSpaceSpriteCommand(
                boxComponent.BackgroundSprite,
                boxComponent.Transparency ?? 1f,
                uiTransformComponent.Position,
                boxComponent.Dimensions));

            if (maybeChildrenComponent.HasComponent)
            {
                foreach (var childEntity in maybeChildrenComponent.Component.Children)
                {
                    RenderEntityAndChildren(childEntity, uiTransformComponent.Position);
                }
            }
        }
    }
}
