using System.Numerics;
using MyEngine.Assets;
using StbImageSharp;

namespace MyEngine.Rendering;

public class Sprite : ILoadableAsset
{
    private Sprite(AssetId id, Vector2 dimensions, uint pixelsPerUnit, byte[] data)
    {
        Id = id;
        Dimensions = dimensions;
        PixelsPerUnit = pixelsPerUnit;
        Data = data;
    }

    public AssetId Id { get; }

    public Vector2 Dimensions { get; }

    public uint PixelsPerUnit { get; }

    public byte[] Data { get; }

    public static async Task<IAsset> LoadAsync(AssetId id, Stream stream)
    {
        var buffer = new byte[stream.Length];
        await stream.ReadAsync(buffer.AsMemory(0, (int)stream.Length));
        var imageResult = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);
        return new Sprite(id, new Vector2(imageResult.Width, imageResult.Height), 100, imageResult.Data);
    }
}
