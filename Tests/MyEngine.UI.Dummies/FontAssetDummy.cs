using FakeItEasy;
using MyEngine.Assets;
using MyEngine.Rendering;

namespace MyEngine.UI.Dummies;

public class FontAssetDummy : DummyFactory<FontAsset>
{
    protected override FontAsset Create()
    {
        var texture = A.Dummy<Texture>();
        return FontAsset.Create(AssetId.Generate(), new CreateFontFromLoadedTexture(texture, Array.Empty<FontCharTextureAtlasPiece>()));
    }
}
