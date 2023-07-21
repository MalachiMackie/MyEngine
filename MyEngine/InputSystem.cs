using MyEngine.Core.Ecs.Resources;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Core.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Runtime
{
    internal class InputSystem : ISystem
    {
        private InputResource _inputResource;
        private MyInput _input;

        public InputSystem(InputResource inputResource, MyInput input)
        {
            _inputResource = inputResource;
            _input = input;
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

                if (_input.IsKeyPressed(key))
                {
                }
            }
        }
    }
}
