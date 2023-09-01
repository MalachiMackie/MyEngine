using MyEngine.Core;

namespace MyEngine.Input;

public class InputPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddStartupSystem<InitializeInputSystem>()
            .AddSystem<InputSystem>()
            .AddResource(new InputResource())
            .AddResource(new MyInput());
    }
}
