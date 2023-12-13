//using System.Numerics;
//using MyEngine.Assets;
//using StbImageSharp;

//namespace MyEngine.Core.Rendering;

//public record TextureLoadData(uint PixelsPerUnit);
//public record TextureCreateData(uint PixelsPerUnit, byte[] Data, uint Width, uint Height);

//public class Texture : ILoadableAsset<Texture, TextureLoadData>, ICreatableAsset<Texture, TextureCreateData>
//{
//    private Texture(AssetId id, Vector2 dimensions, uint pixelsPerUnit, byte[] data)
//    {
//        if (pixelsPerUnit <= 0)
//        {
//            throw new InvalidOperationException("Invalid pixels per unit");
//        }
//        Id = id;
//        PixelsPerUnit = pixelsPerUnit;
//        Dimensions = dimensions;
//        Data = data;
//    }

//    public AssetId Id { get; }

//    public Vector2 Dimensions { get; }

//    public uint PixelsPerUnit { get; }

//    public byte[] Data { get; }

//    public static Texture Create(AssetId id, TextureCreateData createData)
//    {
//        return new Texture(id, new Vector2(createData.Width, createData.Height), createData.PixelsPerUnit, createData.Data);
//    }

//    public static async Task<Texture> LoadAsync(AssetId id, Stream stream, TextureLoadData loadData)
//    {
//        var buffer = new byte[stream.Length];
//        await stream.ReadAsync(buffer.AsMemory(0, (int)stream.Length));
//        var imageResult = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);
//        return Create(id, new TextureCreateData(loadData.PixelsPerUnit, imageResult.Data, (uint)imageResult.Width, (uint)imageResult.Height));
//    }
//}
