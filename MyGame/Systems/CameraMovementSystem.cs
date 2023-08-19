using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;
using MyEngine.Utils;

namespace MyGame.Systems;

public class CameraMovementSystem : ISystem
{
    private readonly InputResource _inputResource;
    private readonly IQuery<Camera3DComponent, TransformComponent> _camera3DQuery;
    private readonly IQuery<Camera2DComponent, TransformComponent> _camera2DQuery;

    public CameraMovementSystem(
        InputResource inputResource,
        IQuery<Camera3DComponent, TransformComponent> camera3dQuery,
        IQuery<Camera2DComponent, TransformComponent> camera2dQuery)
    {
        _inputResource = inputResource;
        _camera3DQuery = camera3dQuery;
        _camera2DQuery = camera2dQuery;
    }

    public void Run(double deltaTime)
    {
        if (!TryMove3D(deltaTime))
        {
            TryMove2D(deltaTime);
        }
    }

    private bool TryMove3D(double deltaTime)
    {
        var components = _camera3DQuery.FirstOrDefault();
        if (components is null)
        {
            return false;
        }

        var cameraTransform = components.Component2.Transform;

        var cameraDirection = cameraTransform.rotation.ToEulerAngles();

        var cameraFront = Vector3.Normalize(cameraDirection);

        var speed = 5.0f * (float)deltaTime;
        if (_inputResource.Keyboard.IsKeyDown(MyKey.W))
        {
            cameraTransform.position += speed * cameraFront;
        }
        if (_inputResource.Keyboard.IsKeyDown(MyKey.S))
        {
            cameraTransform.position -= speed * cameraFront;
        }
        if (_inputResource.Keyboard.IsKeyDown(MyKey.A))
        {
            cameraTransform.position -= speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
        }
        if (_inputResource.Keyboard.IsKeyDown(MyKey.D))
        {
            cameraTransform.position += speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
        }

        var lookSensitivity = 0.1f;
        var mouseDelta = _inputResource.MouseDelta;

        cameraDirection.X += mouseDelta.X * lookSensitivity;
        cameraDirection.Y -= mouseDelta.Y * lookSensitivity;

        cameraTransform.rotation = MathHelper.ToQuaternion(cameraDirection);

        return true;
    }

    private bool TryMove2D(double deltaTime)
    {
        var components = _camera2DQuery.FirstOrDefault();
        if (components is null)
        {
            return false;
        }

        var cameraTransform = components.Component2.Transform;

        var speed = 5.0f * (float)deltaTime;
        if (_inputResource.Keyboard.IsKeyDown(MyKey.W))
        {
            cameraTransform.position += speed * Vector3.UnitY;
        }
        if (_inputResource.Keyboard.IsKeyDown(MyKey.S))
        {
            cameraTransform.position -= speed * Vector3.UnitY;
        }
        if (_inputResource.Keyboard.IsKeyDown(MyKey.A))
        {
            cameraTransform.position -= speed * Vector3.UnitX;
        }
        if (_inputResource.Keyboard.IsKeyDown(MyKey.D))
        {
            cameraTransform.position += speed * Vector3.UnitX;
        }

        return true;
    }
}
