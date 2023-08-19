﻿using System.Numerics;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;

namespace MyGame.Systems;

public class MoveBallSystem : ISystem
{
    private readonly IQuery<BallComponent, KinematicBody2DComponent> _playerQuery;
    private readonly InputResource _inputResource;

    public MoveBallSystem(
        IQuery<BallComponent, KinematicBody2DComponent> playerQuery,
        InputResource inputResource)
    {
        _playerQuery = playerQuery;
        _inputResource = inputResource;
    }

    public void Run(double deltaTime)
    {
        if (_inputResource.Keyboard.IsKeyPressed(MyEngine.Core.Input.MyKey.T))
        {
            var components = _playerQuery.FirstOrDefault();
            if (components is null)
            {
                return;
            }

            var (_, kinematicBody) = components;
            kinematicBody.Velocity += Vector2.Normalize(new Vector2(1f, 1f)) * 2f;
        }
    }
}