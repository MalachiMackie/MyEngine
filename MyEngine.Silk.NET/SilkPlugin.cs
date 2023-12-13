﻿using System.Runtime.CompilerServices;
using MyEngine.Core;

[assembly: InternalsVisibleTo("MyEngine.Runtime")]

namespace MyEngine.Silk.NET;
public class SilkPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddStartupSystem<InitializeViewSystem>()
            .AddStartupSystem<InitializeRendererSystem>()
            .AddStartupSystem<InitializeInputSystem>()
            .AddSystem<InputSyncSystem>(PostUpdateSystemStage.Instance);
    }
}
