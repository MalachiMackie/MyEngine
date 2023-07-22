﻿using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Runtime
{
    internal class RenderSystem : IRenderSystem
    {
        private readonly Renderer _renderer;
        private readonly MyQuery<CameraComponent, TransformComponent> _cameraQuery;

        public RenderSystem(
            Renderer renderer,
            MyQuery<CameraComponent, TransformComponent> cameraQuery)
        {
            _renderer = renderer;
            _cameraQuery = cameraQuery;
        }

        public void Render(double deltaTime)
        {
            var (_, transformComponent) = _cameraQuery.FirstOrDefault();
            if (transformComponent is null)
            {
                return;
            }

            _renderer.Render(transformComponent.Transform);
        }
    }
}