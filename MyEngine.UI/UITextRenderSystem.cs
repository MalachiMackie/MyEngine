using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Rendering;
using MyEngine.UI;

namespace MyEngine.Rendering.RenderSystems;

public class UITextRenderSystem : ISystem
{
    private readonly RenderCommandQueue _renderCommandQueue;
    private readonly IQuery<UICanvasComponent, ChildrenComponent> _canvasQuery;
    private readonly IQuery<UITextComponent, UITransformComponent> _textQuery;

    public UITextRenderSystem(IQuery<UITextComponent, UITransformComponent> textQuery,
        IQuery<UICanvasComponent, ChildrenComponent> canvasQuery,
        RenderCommandQueue renderCommandQueue)
    {
        _textQuery = textQuery;
        _canvasQuery = canvasQuery;
        _renderCommandQueue = renderCommandQueue;
    }

    public void Run(double deltaTime)
    {
        foreach (var (_, childrenComponent) in _canvasQuery)
        {
            foreach (var childEntity in childrenComponent.Children)
            {
                var childText = _textQuery.TryGetForEntity(childEntity);
                if (childText is not null)
                {
                    var (textComponent, uiTransformComponent) = childText;
                    _renderCommandQueue.Enqueue(new RenderScreenSpaceTextCommand(
                        textComponent.Font.Texture,
                        textComponent.Font.CharSprites,
                        textComponent.Text,
                        uiTransformComponent.Position));
                }
            }
        }
    }
}
