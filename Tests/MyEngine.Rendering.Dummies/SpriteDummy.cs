using System.Numerics;
using FakeItEasy;
using MyEngine.Assets;

namespace MyEngine.Rendering.Dummies;

public class SpriteDummy : DummyFactory<Sprite>
{
    protected override Sprite Create()
    {
        var texture = A.Dummy<Texture>();
        return Sprite.Create(AssetId.Generate(), new CreateSpriteFromTextureAtlas(texture, new Vector2(0, 0), new Vector2(1, 1), SpriteOrigin.Center));
    }
}
