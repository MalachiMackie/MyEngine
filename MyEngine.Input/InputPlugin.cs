using MyEngine.Core;

namespace MyEngine.Input;

public class InputPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddSystemStage(InputSystemStage.Instance, 1)
            .AddStartupSystem<InitializeInputSystem>()
            .AddSystem<InputSystem>(InputSystemStage.Instance)
            .AddResource(new InputResource())
            .AddResource(new MyInput());
    }
}
