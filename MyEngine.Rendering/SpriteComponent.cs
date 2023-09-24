using System.Numerics;
using MyEngine.Core.Ecs.Components;
using MyEngine.Utils;

namespace MyEngine.Rendering;

public class SpriteComponent : IComponent
{
    public Texture Texture { get; }
    public Vector2[] TextureCoordinates { get; }
    internal int TextureCoordinatesHash { get; }

    public Vector2 WorldDimensions { get; }

    public SpriteComponent(Texture texture, Vector2 topLeftTextureCoordinate, Vector2 atlasSliceDimensions)
    {
        WorldDimensions = new Vector2(
            atlasSliceDimensions.X / texture.PixelsPerUnit,
            atlasSliceDimensions.Y / texture.PixelsPerUnit);

        topLeftTextureCoordinate = new Vector2(
            topLeftTextureCoordinate.X / texture.Dimensions.X,
            topLeftTextureCoordinate.Y / texture.Dimensions.X);

        atlasSliceDimensions = new Vector2(
            atlasSliceDimensions.X / texture.Dimensions.X,
            atlasSliceDimensions.Y / texture.Dimensions.Y);

        if (topLeftTextureCoordinate.X < 0
            || topLeftTextureCoordinate.Y < 0)
        {
            throw new InvalidOperationException("TopLeftTextureCoordinate is invalid");
        }

        if (topLeftTextureCoordinate.X + atlasSliceDimensions.X > 1)
        {
            throw new InvalidOperationException("Invalid sprite width");
        }
        if (topLeftTextureCoordinate.Y + atlasSliceDimensions.Y > 1)
        {
            throw new InvalidOperationException("Invalid sprite height");
        }

        Texture = texture;
        var topLeft = topLeftTextureCoordinate;
        var bottomRight = topLeftTextureCoordinate + atlasSliceDimensions;
        TextureCoordinates = new[]
        {
            new Vector2(bottomRight.X, bottomRight.Y),
            new Vector2(bottomRight.X, topLeft.Y),
            new Vector2(topLeft.X, topLeft.Y),
            new Vector2(topLeft.X, bottomRight.Y)
        };
        TextureCoordinatesHash = HashCode.Combine(
            TextureCoordinates[0],
            TextureCoordinates[1],
            TextureCoordinates[2],
            TextureCoordinates[3],
            WorldDimensions);
    }
    public SpriteComponent(Texture texture) : this(texture, Vector2.Zero, texture.Dimensions)
    {

    }
}
