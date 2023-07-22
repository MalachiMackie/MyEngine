using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyGame.Systems
{
    public class CameraMovementSystem : ISystem<TransformComponent, CameraComponent>
    {
        private InputResource _inputResource { get; }

        public CameraMovementSystem(InputResource inputResource)
        {
            _inputResource = inputResource;
        }

        public void Run(double deltaTime, TransformComponent transformComponent, CameraComponent _)
        {
            var cameraTransform = transformComponent.Transform;
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

            var q = cameraTransform.rotation;

            var direction = MathHelper.ToEulerAngles(q);

            direction.X += mouseDelta.X * lookSensitivity;
            direction.Y -= mouseDelta.Y * lookSensitivity;

            cameraTransform.rotation = MathHelper.ToQuaternion(direction);
        }
    }
}
