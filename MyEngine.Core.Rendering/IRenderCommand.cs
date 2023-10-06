using System.Numerics;

namespace MyEngine.Core.Rendering;

public interface IRenderCommand
{
}

public record RenderSpriteCommand(Sprite Sprite, GlobalTransform GlobalTransform) : IRenderCommand;
public record RenderLineCommand(Vector3 Start, Vector3 End) : IRenderCommand;
public record RenderScreenSpaceTextCommand(
    Texture Texture,
    IReadOnlyDictionary<char, Sprite> CharacterSprites,
    string Text,
    float Transparency,
    Vector2 Position) : IRenderCommand;

public record RenderScreenSpaceSpriteCommand(
    Sprite Sprite,
    float Transparency,
    Vector2 Position,
    Vector2 Dimensions) : IRenderCommand;
