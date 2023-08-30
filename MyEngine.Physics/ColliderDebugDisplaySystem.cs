using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Utils;

namespace MyEngine.Physics;

public class ColliderDebugDisplaySystem : ISystem
{
    private readonly MyPhysics _myPhysics;
    private readonly DebugColliderDisplayResource _debugColliderDisplayResource;
    private readonly ILineRenderResource _lineRenderResource;

    public ColliderDebugDisplaySystem(MyPhysics myPhysics, ILineRenderResource lineRenderResource, DebugColliderDisplayResource debugColliderDisplayResource)
    {
        _myPhysics = myPhysics;
        _lineRenderResource = lineRenderResource;
        _debugColliderDisplayResource = debugColliderDisplayResource;
    }

    public void Run(double deltaTime)
    {
        if (!_debugColliderDisplayResource.DisplayColliders)
        {
            return;
        }

        var colliderPositions = _myPhysics.GetAllColliderPositions();
        foreach (var collider in colliderPositions)
        {
            collider.Collider.Match(
                x => RenderBox2D(x, collider.Position.XY(), collider.Rotation),
                x => RenderCircle2D(x, collider.Position.XY()));
        }
    }

    private void RenderBox2D(BoxCollider2D boxCollider, Vector2 position, Quaternion rotation)
    {
        var halfDimensions = boxCollider.Dimensions * 0.5f;
        var bottomLeftLocal = new Vector2(-halfDimensions.X, -halfDimensions.Y);
        var topLeftLocal = new Vector2(-halfDimensions.X, halfDimensions.Y);
        var bottomRightLocal = new Vector2(halfDimensions.X, -halfDimensions.Y);
        var topRightLocal = new Vector2(halfDimensions.X, halfDimensions.Y);

        var matrix = new GlobalTransform(position.Extend(1f), rotation, Vector3.One)
            .ModelMatrix;

        var bottomLeftWorld = (matrix * Matrix4x4.CreateTranslation(bottomLeftLocal.Extend(1f))).Translation;
        var topLeftWorld = (matrix * Matrix4x4.CreateTranslation(topLeftLocal.Extend(1f))).Translation;
        var bottomRightWorld = (matrix * Matrix4x4.CreateTranslation(bottomRightLocal.Extend(1f))).Translation;
        var topRightWorld = (matrix * Matrix4x4.CreateTranslation(topRightLocal.Extend(1f))).Translation;

        _lineRenderResource.RenderLine(bottomLeftWorld, topLeftWorld);
        _lineRenderResource.RenderLine(topLeftWorld, topRightWorld);
        _lineRenderResource.RenderLine(topRightWorld, bottomRightWorld);
        _lineRenderResource.RenderLine(bottomRightWorld, bottomLeftWorld);
    }

    private void RenderCircle2D(CircleCollider2D circleCollider, Vector2 position)
    {
        var result = _lineRenderResource.RenderLineCircle(position.Extend(1f), circleCollider.Radius);
        if (result.TryGetError(out var error))
        {
            Console.WriteLine("Failed to render circle collider: {0}", error);
        }
    }
}
