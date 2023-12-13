using System.Numerics;
using MyEngine.Assets;

namespace MyEngine.Rendering;

public record CreateSpriteFromTextureAtlas(Texture Texture, Vector2 BottomLeftTextureCoordinate, Vector2 AtlasPieceDimensions, SpriteOrigin Origin);

public record CreateSpriteFromFullTexture(uint PixelsPerUnit, SpriteOrigin Origin);

public enum SpriteOrigin
{
    Center,
    TopLeft,
    BottomLeft,
    TopRight,
    BottomRight
}

public class Sprite : ICreatableAsset<Sprite, CreateSpriteFromTextureAtlas>, ILoadableAsset<Sprite, CreateSpriteFromFullTexture>
{
    public SpriteOrigin Origin { get; }
    public Texture Texture { get; }
    public Vector2[] TextureCoordinates { get; }
    internal int SpriteHash { get; }

    public Vector2 WorldDimensions { get; }
    public Vector2 Dimensions { get; }

    public AssetId Id { get; }

    private Sprite(
        Texture texture,
        Vector2[] textureCoordinates,
        int spriteHash,
        Vector2 worldDimensions,
        Vector2 dimensions,
        AssetId id,
        SpriteOrigin origin)
    {
        Texture = texture;
        TextureCoordinates = textureCoordinates;
        SpriteHash = spriteHash;
        Dimensions = dimensions;
        WorldDimensions = worldDimensions;
        Id = id;
        Origin = origin;
    }

    public static Sprite Create(AssetId id, CreateSpriteFromTextureAtlas createData)
    {
        var (texture, bottomLeftTextureCoordinate, atlasPieceDimensions, origin) = createData;

        var worldDimensions = new Vector2(
            atlasPieceDimensions.X / texture.PixelsPerUnit,
            atlasPieceDimensions.Y / texture.PixelsPerUnit);

        // add half a pixel, so the texture coordinate is in the middle of the pixel, rather than on the edge
        var normalizedBottomLeftTextureCoordinate = new Vector2(
            (bottomLeftTextureCoordinate.X + 0.5f) / texture.Dimensions.X,
            (bottomLeftTextureCoordinate.Y + 0.5f) / texture.Dimensions.Y);

        var normalizedAtlasPieceDimensions = new Vector2(
            (atlasPieceDimensions.X - 1f) / texture.Dimensions.X,
            (atlasPieceDimensions.Y - 1f) / texture.Dimensions.Y);

        if (normalizedBottomLeftTextureCoordinate.X < 0
            || normalizedBottomLeftTextureCoordinate.Y < 0)
        {
            throw new InvalidOperationException("TopLeftTextureCoordinate is invalid");
        }

        if (normalizedBottomLeftTextureCoordinate.X + normalizedAtlasPieceDimensions.X > 1)
        {
            throw new InvalidOperationException("Invalid sprite width");
        }
        if (normalizedBottomLeftTextureCoordinate.Y + normalizedAtlasPieceDimensions.Y > 1)
        {
            throw new InvalidOperationException("Invalid sprite height");
        }

        var bottomLeft = normalizedBottomLeftTextureCoordinate;
        var topRight = normalizedBottomLeftTextureCoordinate + normalizedAtlasPieceDimensions;
        var textureCoordinates = new[]
        {
            new Vector2(topRight.X, bottomLeft.Y),
            new Vector2(topRight.X, topRight.Y),
            new Vector2(bottomLeft.X, topRight.Y),
            new Vector2(bottomLeft.X, bottomLeft.Y),
        };
        var hash = HashCode.Combine(
            textureCoordinates[0],
            textureCoordinates[1],
            textureCoordinates[2],
            textureCoordinates[3],
            worldDimensions,
            origin);

        return new Sprite(texture, textureCoordinates, hash, worldDimensions, atlasPieceDimensions, id, origin);
    }

    public static async Task<Sprite> LoadAsync(AssetId id, Stream stream, CreateSpriteFromFullTexture loadData)
    {
        var texture = await Texture.LoadAsync(AssetId.Generate(), stream, new TextureLoadData(loadData.PixelsPerUnit));
        return Create(id, new CreateSpriteFromTextureAtlas(texture, Vector2.Zero, texture.Dimensions, loadData.Origin));
    }
}
