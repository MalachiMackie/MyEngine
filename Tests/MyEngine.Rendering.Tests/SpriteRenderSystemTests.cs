using System.Numerics;
using FakeItEasy;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Rendering.RenderSystems;

namespace MyEngine.Rendering.Tests;

public class SpriteRenderSystemTests
{
    private readonly IRenderCommandQueue _commandQueue = A.Fake<IRenderCommandQueue>();
    private readonly IQuery<SpriteComponent, TransformComponent, OptionalComponent<TransparencyComponent>> _query =
        A.Fake<IQuery<SpriteComponent, TransformComponent, OptionalComponent<TransparencyComponent>>>();
    private readonly SpriteRenderSystem _system;

    public SpriteRenderSystemTests()
    {
        _system = new SpriteRenderSystem(_commandQueue, _query);
    }

    [Fact]
    public void ShouldEnqueueSprites()
    {
        var spriteComponent = new SpriteComponent(A.Dummy<Sprite>());
        var transform1 = new Transform(new Vector3(0f, 1f, 2f), Quaternion.Identity, Vector3.One);
        var transform2 = new Transform(new Vector3(1f, 2f, 3f), Quaternion.Identity, Vector3.One);
        A.CallTo(() => _query.GetEnumerator())
            .Returns(new[] {
                new EntityComponents<SpriteComponent, TransformComponent, OptionalComponent<TransparencyComponent>>(EntityId.Generate())
                {
                    Component1 = spriteComponent,
                    Component2 = new TransformComponent(transform1),
                    Component3 = new OptionalComponent<TransparencyComponent>(null)
                },
                new EntityComponents<SpriteComponent, TransformComponent, OptionalComponent<TransparencyComponent>>(EntityId.Generate())
                {
                    Component1 = spriteComponent,
                    Component2 = new TransformComponent(transform2),
                    Component3 = new OptionalComponent<TransparencyComponent>(new TransparencyComponent(){ Transparency = 0.5f })
                },
            }.AsEnumerable().GetEnumerator());

        _system.Run(1);

        A.CallTo(() => _commandQueue.Enqueue(new RenderSpriteCommand(spriteComponent.Sprite, spriteComponent.Dimensions, GlobalTransform.FromTransform(transform1), 1f)))
            .MustHaveHappened();

        A.CallTo(() => _commandQueue.Enqueue(new RenderSpriteCommand(spriteComponent.Sprite, spriteComponent.Dimensions, GlobalTransform.FromTransform(transform2), 0.5f)))
            .MustHaveHappened();
    }
}
