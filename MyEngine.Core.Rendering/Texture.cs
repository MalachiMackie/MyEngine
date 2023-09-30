using System.Numerics;
using MyEngine.Assets;
using StbImageSharp;

namespace MyEngine.Core.Rendering;

public record TextureLoadData(uint PixelsPerUnit);

public class Texture : ILoadableAsset<Texture, TextureLoadData>
{
    private Texture(AssetId id, Vector2 dimensions, uint pixelsPerUnit, byte[] data)
    {
        if (pixelsPerUnit <= 0)
        {
            throw new InvalidOperationException("Invalid pixels per unit");
        }
        Id = id;
        PixelsPerUnit = pixelsPerUnit;
        Dimensions = dimensions;
        Data = data;
    }

    public AssetId Id { get; }

    public Vector2 Dimensions { get; }

    public uint PixelsPerUnit { get; }

    public byte[] Data { get; }

    public static async Task<Texture> LoadAsync(AssetId id, Stream stream, TextureLoadData loadData)
    {
        var buffer = new byte[stream.Length];
        await stream.ReadAsync(buffer.AsMemory(0, (int)stream.Length));
        var imageResult = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);
        return new Texture(id, new Vector2(imageResult.Width, imageResult.Height), loadData.PixelsPerUnit, imageResult.Data);
    }
}
