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
    private readonly IQuery<UITextComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>> _textQuery;
    private readonly IQuery<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>> _boxQuery;

    public UIRenderSystem(IQuery<UITextComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>> textQuery,
        IQuery<UICanvasComponent, ChildrenComponent> canvasQuery,
        RenderCommandQueue renderCommandQueue,
        IQuery<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>> boxQuery)
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
                RenderEntityAndChildren(childEntity, new Vector3());
            }
        }
    }

    private void RenderEntityAndChildren(EntityId entity, Vector3 position)
    {
        var textComponents = _textQuery.TryGetForEntity(entity);
        if (textComponents is not null)
        {
            var (textComponent, uiTransformComponent, maybeChildrenComponent, maybeTransparency) = textComponents;

            position += uiTransformComponent.Position;

            _renderCommandQueue.Enqueue(new RenderScreenSpaceTextCommand(
                textComponent.Font.Texture,
                textComponent.Font.CharSprites,
                textComponent.Text,
                maybeTransparency.Component?.Transparency ?? 1f,
                position));

            TryRenderChildren(maybeChildrenComponent, position);
        }
        var boxComponents = _boxQuery.TryGetForEntity(entity);
        if (boxComponents is not null)
        {
            var (boxComponent, uiTransformComponent, maybeChildrenComponent, maybeTransparency) = boxComponents;

            position += uiTransformComponent.Position;

            _renderCommandQueue.Enqueue(new RenderScreenSpaceSpriteCommand(
                boxComponent.BackgroundSprite,
                maybeTransparency.Component?.Transparency ?? 1f,
                position,
                boxComponent.Dimensions));

            TryRenderChildren(maybeChildrenComponent, position);
        }
    }

    private void TryRenderChildren(OptionalComponent<ChildrenComponent> maybeChildrenComponent, Vector3 parentPosition)
    {
        if (maybeChildrenComponent.HasComponent)
        {
            var childPosition = new Vector3(
                parentPosition.X,
                parentPosition.Y,
                parentPosition.Z + 1);
            foreach (var childEntity in maybeChildrenComponent.Component.Children)
            {
                RenderEntityAndChildren(childEntity, childPosition);
            }
        }
    }
}
