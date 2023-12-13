using System.Numerics;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Rendering;

public interface IRenderer : IResource, IDisposable
{

    public record struct RendererStats(uint DrawCalls);

    public void Resize(uint width, uint height);

    public RendererStats RenderOrthographic(
        Vector3 cameraPosition,
        Vector2 viewSize,
        IEnumerable<SpriteRender> sprites,
        IEnumerable<SpriteRender> screenSprites,
        IEnumerable<LineRender> lines,
        IEnumerable<TextRender> textRenders);

    public sealed record TextRender(Vector3 Position,
        string Text,
        float Transparency,
        Texture Texture,
        float LineHeight,
        float SpaceWidth,
        IReadOnlyDictionary<char, Sprite> CharacterSprites);

    public readonly record struct LineRender(Vector3 Start, Vector3 End);

    public readonly record struct SpriteRender(
        Sprite Sprite,
        Vector2 Dimensions,
        float Transparency,
        Matrix4x4 ModelMatrix
        )
    {
        public Vector3 Position => ModelMatrix.Translation;
    };
}

