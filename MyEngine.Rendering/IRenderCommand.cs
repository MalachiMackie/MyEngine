using System.Numerics;
using MyEngine.Core;

namespace MyEngine.Rendering;

public interface IRenderCommand
{
}

public record RenderSpriteCommand(Sprite Sprite, Vector2 Dimensions, GlobalTransform GlobalTransform, float Transparency) : IRenderCommand;
public record RenderLineCommand(Vector3 Start, Vector3 End) : IRenderCommand;
public record RenderScreenSpaceTextCommand(
    Texture Texture,
    IReadOnlyDictionary<char, Sprite> CharacterSprites,
    string Text,
    float Transparency,
    Vector3 Position) : IRenderCommand;

public record RenderScreenSpaceSpriteCommand(
    Sprite Sprite,
    float Transparency,
    Vector3 Position,
    Vector2 Dimensions) : IRenderCommand;
