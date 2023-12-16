using FakeItEasy;
using MyEngine.Assets;

namespace MyEngine.Rendering.Dummies;
public class TextureDummy : DummyFactory<Texture>
{
    protected override Texture Create()
    {
        return Texture.Create(AssetId.Generate(), new TextureCreateData(1, new byte[] { 255, 255, 255, 255 }, 1, 1));
    }
}
