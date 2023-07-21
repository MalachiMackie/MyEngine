using System.Numerics;
using MyEngine.Core;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyGame.Systems
{
    public class CameraMovementSystem : ISystem<TransformComponent, CameraComponent, InputResource>
    {
        public void Run(double deltaTime, TransformComponent transformComponent, CameraComponent _, InputResource inputResource)
        {
            var cameraTransform = transformComponent.Transform;
            var cameraDirection = MathHelper.ToEulerAngles(cameraTransform.rotation);

            var cameraFront = Vector3.Normalize(cameraDirection);

            var speed = 5.0f * (float)deltaTime;
            if (inputResource.Keyboard.IsKeyDown(MyKey.W))
            {
                cameraTransform.position += speed * cameraFront;
            }
            if (inputResource.Keyboard.IsKeyDown(MyKey.S))
            {
                cameraTransform.position -= speed * cameraFront;
            }
            if (inputResource.Keyboard.IsKeyDown(MyKey.A))
            {
                cameraTransform.position -= speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
            }
            if (inputResource.Keyboard.IsKeyDown(MyKey.D))
            {
                cameraTransform.position += speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
            }

            var lookSensitivity = 0.1f;
            var mouseDelta = inputResource.MouseDelta;

            var q = cameraTransform.rotation;

            var direction = MathHelper.ToEulerAngles(q);

            direction.X += mouseDelta.X * lookSensitivity;
            direction.Y -= mouseDelta.Y * lookSensitivity;

            cameraTransform.rotation = MathHelper.ToQuaternion(direction);

        }
    }
}
