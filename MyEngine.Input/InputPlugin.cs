using System.Runtime.CompilerServices;
using MyEngine.Core;

[assembly: InternalsVisibleTo("MyEngine.ImGui")]

namespace MyEngine.Input;

public class InputPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddSystemStage(InputSystemStage.Instance, 1)
            .AddStartupSystem<InitializeInputSystem>()
            .AddSystem<InputSystem>(InputSystemStage.Instance)
            .AddResource(new InputResource());
    }
}
