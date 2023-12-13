using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;
using Silk.NET.OpenGL;

namespace MyEngine.Silk.NET;
internal class InitializeRendererSystem : IStartupSystem
{
    private readonly SilkView _silkView;
    private readonly ResourceRegistrationResource _resourceRegistrationResource;

    public InitializeRendererSystem(ResourceRegistrationResource resourceRegistrationResource, SilkView silkView)
    {
        _resourceRegistrationResource = resourceRegistrationResource;
        _silkView = silkView;
    }

    public void Run()
    {
        _silkView.AddLoadAction(OnLoad);
    }

    private void OnLoad()
    {
        var openGL = _silkView.View.CreateOpenGL();
        var rendererResult = OpenGLRenderer.Create(openGL);
        if (!rendererResult.TryGetValue(out var renderer))
        {
            Console.WriteLine("Failed to create renderer: {0}", string.Join(";", rendererResult.GetErrors()));
            return;
        }

        _silkView.View.Resize += (e) =>
        {
            renderer.Resize((uint)e.X, (uint)e.Y);
        };

        _resourceRegistrationResource.AddResource<IRenderer>(renderer);
    }
}
