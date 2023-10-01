using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core.Rendering;

namespace MyEngine.UI;

public record FontCharTextureAtlasPiece(char Char, Vector2 TopLeftCoordinate, Vector2 AtlasPieceDimensions);

public record CreateFontFromLoadedTexture(Texture Texture, IEnumerable<FontCharTextureAtlasPiece> CharAtlasPieces);

public class FontAsset : ICreatableAsset<FontAsset, CreateFontFromLoadedTexture>, ILoadableAsset<FontAsset>, IEquatable<FontAsset>
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
                new CreateSpriteFromTextureAtlas(texture, x.TopLeftCoordinate, x.AtlasPieceDimensions, SpriteOrigin.BottomLeft)));

        return new FontAsset(id, texture, charSprites);
    }

    public static async Task<FontAsset> LoadAsync(AssetId id, Stream stream)
    {
        var texture = await Texture.LoadAsync(AssetId.Generate(), stream, new TextureLoadData(100));
        return Create(id, new CreateFontFromLoadedTexture(
            texture,
            new FontCharTextureAtlasPiece[]
            {
                new('A', new Vector2(164, 140), new Vector2(15, 24)),
                new('B', new Vector2(188, 140), new Vector2(15, 24)),
                new('C', new Vector2(234, 140), new Vector2(15, 24)),
                new('D', new Vector2(4, 172), new Vector2(15, 24)),
                new('E', new Vector2(27, 172), new Vector2(15, 24)),
                new('F', new Vector2(50, 172), new Vector2(15, 24)),
                new('G', new Vector2(73, 172), new Vector2(15, 24)),
                new('H', new Vector2(96, 140), new Vector2(15, 24)),
                new('I', new Vector2(119, 172), new Vector2(14, 24)),
                new('J', new Vector2(55, 44), new Vector2(15, 24)),
                new('K', new Vector2(78, 44), new Vector2(15, 24)),
                new('L', new Vector2(101, 44), new Vector2(15, 24)),
                new('M', new Vector2(124, 44), new Vector2(15, 24)),
                new('N', new Vector2(147, 44), new Vector2(15, 24)),
                new('O', new Vector2(4, 76), new Vector2(15, 24)),
                new('P', new Vector2(170, 44), new Vector2(15, 24)),
                new('Q', new Vector2(193, 44), new Vector2(15, 24)),
                new('R', new Vector2(216, 44), new Vector2(15, 24)),
                new('S', new Vector2(27, 76), new Vector2(15, 24)),
                new('T', new Vector2(50, 76), new Vector2(15, 24)),
                new('U', new Vector2(73, 76), new Vector2(15, 24)),
                new('V', new Vector2(97, 76), new Vector2(15, 24)),
                new('W', new Vector2(119, 76), new Vector2(15, 24)),
                new('X', new Vector2(32, 44), new Vector2(15, 24)),
                new('Y', new Vector2(165, 76), new Vector2(15, 24)),
                new('Z', new Vector2(188, 76), new Vector2(15, 24)),
            }));
    }

    public bool Equals(FontAsset? other)
    {
        return other is not null && other.Id == Id;
    }
}
