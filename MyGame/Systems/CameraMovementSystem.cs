using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyGame.Systems
{
    public class CameraMovementSystem : ISystem
    {
        private readonly InputResource _inputResource;
        private readonly MyQuery<CameraComponent, TransformComponent> _cameraQuery;

        public CameraMovementSystem(
            InputResource inputResource,
            MyQuery<CameraComponent, TransformComponent> cameraQuery)
        {
            _inputResource = inputResource;
            _cameraQuery = cameraQuery;
        }

        public void Run(double deltaTime)
        {
            var (_, transformComponent) = _cameraQuery.FirstOrDefault();
            if (transformComponent is null)
            {
                return;
            }

            Move2D(transformComponent.Transform, deltaTime);
        }

        private void Move3D(Transform cameraTransform, double deltaTime)
        {
            var cameraDirection = MathHelper.ToEulerAngles(cameraTransform.rotation);

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

        }

        private void Move2D(Transform cameraTransform, double deltaTime)
        {
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
        }
    }
}
