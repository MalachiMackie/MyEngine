using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;

namespace MyEngine.Runtime;

internal class InputSystem : ISystem
{
    private readonly MyInput _input;
    private readonly InputResource _inputResource;

    public InputSystem(
        MyInput input,
        InputResource inputResource)
    {
        _input = input;
        _inputResource = inputResource;

    }

    public void Run(double deltaTime)
    {
        UpdateMouse();
        UpdateKeyboard();
    }

    private void UpdateMouse()
    {
        var newMousePosition = _input.Mouse.Position;
        var previousMousePosition = _inputResource.Mouse.Position;
        if (previousMousePosition != default)
        {
            _inputResource.MouseDelta = newMousePosition - previousMousePosition;
        }
        _inputResource.Mouse.Position = newMousePosition;
    }

    private void UpdateKeyboard()
    {
        var keyStates = _inputResource.Keyboard.InternalKeyStates;
        foreach (var key in keyStates.Keys)
        {
            var currentKeyState = keyStates[key];
            var isKeyPressed = _input.IsKeyPressed(key);
            switch (currentKeyState)
            {
                case KeyState.Pressed:
                    {
                        keyStates[key] = isKeyPressed ? KeyState.Held : KeyState.Released;
                        break;
                    }
                case KeyState.Held:
                    {
                        if (!isKeyPressed)
                        {
                            keyStates[key] = KeyState.Released;
                        }
                        break;
                    }
                case KeyState.Released:
                    {
                        keyStates[key] = isKeyPressed ? KeyState.Pressed : KeyState.NotPressed;
                        break;
                    }
                case KeyState.NotPressed:
                    {
                        if (isKeyPressed)
                        {
                            keyStates[key] = KeyState.Pressed;
                        }
                        break;
                    }
            }
        }
    }
}
