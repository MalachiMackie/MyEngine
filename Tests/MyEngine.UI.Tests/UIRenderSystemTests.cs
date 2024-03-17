using System.Numerics;
using FakeItEasy;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Rendering;
using MyEngine.Rendering.RenderSystems;

namespace MyEngine.UI.Tests;

public class UIRenderSystemTests
{
    private readonly UIRenderSystem _system;
    private readonly IQuery<UITextComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>> _textQuery =
        A.Fake<IQuery<UITextComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>>>();
    private readonly IQuery<UICanvasComponent, ChildrenComponent> _canvasQuery = 
        A.Fake<IQuery<UICanvasComponent, ChildrenComponent>>();
    private readonly IRenderCommandQueue _renderCommandQueue = 
        A.Fake<IRenderCommandQueue>();
    private readonly IQuery<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>> _boxQuery = 
        A.Fake<IQuery<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>>>();

    public UIRenderSystemTests()
    {
        _system = new UIRenderSystem(_textQuery, _canvasQuery, _renderCommandQueue, _boxQuery);
    }

    [Fact]
    public void Should_EnqueueUIRenderCommands()
    {
        var sprite = A.Dummy<Sprite>();
        var font = A.Dummy<FontAsset>();

        var canvasEntity = EntityId.Generate();
        var boxEntity = EntityId.Generate();
        var textEntity = EntityId.Generate();
        var textEntity2 = EntityId.Generate();

        var canvasChildren = new ChildrenComponent();
        canvasChildren.AddChild(boxEntity);

        var boxChildren = new ChildrenComponent();
        boxChildren.AddChild(textEntity);
        
        var textChildren = new ChildrenComponent();
        textChildren.AddChild(textEntity2);

        A.CallTo(() => _canvasQuery.GetEnumerator()).Returns(new[] {
            new EntityComponents<UICanvasComponent, ChildrenComponent>(canvasEntity)
            {
                Component1 = new UICanvasComponent(),
                Component2 = canvasChildren
            }
        }.AsEnumerable().GetEnumerator());

        A.CallTo(() => _boxQuery.TryGetForEntity(boxEntity)).Returns(
            new EntityComponents<UIBoxComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>>(boxEntity)
            {
                Component1 = new UIBoxComponent(){Dimensions = new Vector2(1f, 1f), BackgroundSprite = sprite},
                Component2 = new UITransformComponent(){Position = new Vector3(1f, 2f, 3f)},
                Component3 = new OptionalComponent<ChildrenComponent>(boxChildren),
                Component4 = new OptionalComponent<UITransparencyComponent>(new UITransparencyComponent(){Transparency = 0.5f})
            });

        A.CallTo(() => _textQuery.TryGetForEntity(textEntity)).Returns(new EntityComponents<UITextComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>>(textEntity)
            {
                Component1 = new UITextComponent(){Font = font, Text = "Hello World"},
                Component2 = new UITransformComponent(){Position = new Vector3(2f, 3f, 4f)},
                Component3 = new OptionalComponent<ChildrenComponent>(textChildren),
                Component4 = new OptionalComponent<UITransparencyComponent>(new UITransparencyComponent(){Transparency = 0.7f})
            });

        A.CallTo(() => _textQuery.TryGetForEntity(textEntity2)).Returns(new EntityComponents<UITextComponent, UITransformComponent, OptionalComponent<ChildrenComponent>, OptionalComponent<UITransparencyComponent>>(textEntity2)
            {
                Component1 = new UITextComponent(){Font = font, Text = "New Text"},
                Component2 = new UITransformComponent(){Position = new Vector3(3f, 4f, 5f)},
                Component3 = new OptionalComponent<ChildrenComponent>(null),
                Component4 = new OptionalComponent<UITransparencyComponent>(new UITransparencyComponent(){Transparency = 0.8f})
            });

        _system.Run(1);

        A.CallTo(() => _renderCommandQueue.Enqueue(new RenderScreenSpaceSpriteCommand(sprite, 0.5f, new Vector3(1f, 2f, 3f), new Vector2(1f, 1f))))
            .MustHaveHappened();
        // z gets increased every level
        A.CallTo(() => _renderCommandQueue.Enqueue(new RenderScreenSpaceTextCommand(font.Texture, font.CharSprites, "Hello World", 0.7f, new Vector3(3f, 5f, 8f))))
            .MustHaveHappened();
        A.CallTo(() => _renderCommandQueue.Enqueue(new RenderScreenSpaceTextCommand(font.Texture, font.CharSprites, "New Text", 0.8f, new Vector3(6f, 9f, 14f))))
            .MustHaveHappened();

        A.CallTo(() => _renderCommandQueue.Enqueue(An<IRenderCommand>._)).MustHaveHappened(3, Times.Exactly);
    }
}