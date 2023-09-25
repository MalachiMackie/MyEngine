using System.Numerics;
using MyEngine.Assets;
using MyEngine.Rendering;

namespace MyEngine.UI;

public record FontCharTextureAtlasPiece(char Char, Vector2 TopLeftCoordinate, Vector2 AtlasPieceDimensions);

public record CreateFontFromLoadedTexture(Texture Texture, IEnumerable<FontCharTextureAtlasPiece> CharAtlasPieces);

public class FontAsset : ICreatableAsset<FontAsset, CreateFontFromLoadedTexture>, ILoadableAsset<FontAsset>
{
    private FontAsset(AssetId id, Texture texture, IReadOnlyDictionary<char, Sprite> charSprites)
    {
        CharSprites = charSprites;
        Id = id;
        Texture = texture;
    }

    public AssetId Id { get; }

    public Texture Texture { get; }

    public IReadOnlyDictionary<char, Sprite> CharSprites { get; }

    public static FontAsset Create(AssetId id, CreateFontFromLoadedTexture createData)
    {
        var texture = createData.Texture;
        var charSprites = createData.CharAtlasPieces
            .ToDictionary(
            x => x.Char,
            x => Sprite.Create(
                AssetId.Generate(),
                new CreateSpriteFromTextureAtlas(texture, x.TopLeftCoordinate, x.AtlasPieceDimensions)));

        return new FontAsset(id, texture, charSprites);
    }

    public static async Task<FontAsset> LoadAsync(AssetId id, Stream stream)
    {
        var texture = await Texture.LoadAsync(AssetId.Generate(), stream, new TextureLoadData(100));
        return Create(id, new CreateFontFromLoadedTexture(
            texture,
            new[]
            {
                new FontCharTextureAtlasPiece('A', new Vector2(164, 140), new Vector2(15, 23))
            }));
    }
}
