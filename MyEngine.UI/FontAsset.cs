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
                new('A', new Vector2(165, 139), new Vector2(17, 26)),
                new('B', new Vector2(187, 139), new Vector2(17, 26)),
                new('C', new Vector2(233, 139), new Vector2(17, 26)),
                new('D', new Vector2(3, 171), new Vector2(17, 26)),
                new('E', new Vector2(26, 171), new Vector2(17, 26)),
                new('F', new Vector2(49, 171), new Vector2(17, 26)),
                new('G', new Vector2(72, 171), new Vector2(17, 26)),
                new('H', new Vector2(95, 139), new Vector2(17, 26)),
                new('I', new Vector2(118, 171), new Vector2(17, 26)),
                new('J', new Vector2(54, 43), new Vector2(17, 26)),
                new('K', new Vector2(77, 43), new Vector2(17, 26)),
                new('L', new Vector2(100, 43), new Vector2(17, 26)),
                new('M', new Vector2(123, 43), new Vector2(17, 26)),
                new('N', new Vector2(146, 43), new Vector2(17, 26)),
                new('O', new Vector2(3, 75), new Vector2(17, 26)),
                new('P', new Vector2(169, 43), new Vector2(17, 26)),
                new('Q', new Vector2(192, 43), new Vector2(17, 26)),
                new('R', new Vector2(215, 43), new Vector2(17, 26)),
                new('S', new Vector2(26, 75), new Vector2(17, 26)),
                new('T', new Vector2(49, 75), new Vector2(17, 26)),
                new('U', new Vector2(72, 75), new Vector2(17, 26)),
                new('V', new Vector2(96, 75), new Vector2(17, 26)),
                new('W', new Vector2(118, 75), new Vector2(17, 26)),
                new('X', new Vector2(31, 43), new Vector2(17, 26)),
                new('Y', new Vector2(164, 75), new Vector2(17, 26)),
                new('Z', new Vector2(187, 75), new Vector2(17, 26)),
                new('0', new Vector2(96, 171), new Vector2(17, 26)),
                new('1', new Vector2(182, 171), new Vector2(14, 26)),
                new('2', new Vector2(118, 139), new Vector2(17, 26)),
                new('3', new Vector2(72, 139), new Vector2(17, 26)),
                new('4', new Vector2(187, 107), new Vector2(17, 26)),
                new('5', new Vector2(210, 107), new Vector2(17, 26)),
                new('6', new Vector2(233, 107), new Vector2(17, 26)),
                new('7', new Vector2(3, 139), new Vector2(17, 26)),
                new('8', new Vector2(26, 139), new Vector2(17, 26)),
                new('9', new Vector2(49, 139), new Vector2(17, 26)),
                new('.', new Vector2(62, 251), new Vector2(8, 8)),
                new('-', new Vector2(183, 223), new Vector2(17, 13)),
                new(',', new Vector2(22, 251), new Vector2(8, 13)),
            }));
    }

    public bool Equals(FontAsset? other)
    {
        return other is not null && other.Id == Id;
    }
}
