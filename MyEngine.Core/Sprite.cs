namespace MyEngine.Core;

public readonly record struct SpriteId(Guid Value);

public class Sprite
{
    public Sprite(SpriteId id, Vector2 dimensions, uint pixelsPerUnit, byte[] data)
    {
        Id = id;
        Dimensions = dimensions;
        PixelsPerUnit = pixelsPerUnit;
        Data = data;
    }

    public SpriteId Id { get; }

    public Vector2 Dimensions { get; }

    public uint PixelsPerUnit { get; }

    public byte[] Data { get; }
}
