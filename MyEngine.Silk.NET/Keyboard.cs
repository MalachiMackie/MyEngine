using MyEngine.Input;
using ISilkKeyboard = Silk.NET.Input.IKeyboard;
using SilkKey = Silk.NET.Input.Key;

namespace MyEngine.Silk.NET;

internal class Keyboard : IKeyboard
{
    private readonly ISilkKeyboard _silkKeyboard;
    private readonly Dictionary<MyKey, KeyState> _keyStates = Enum.GetValues<MyKey>().ToDictionary(x => x, x => KeyState.NotPressed);

    public Keyboard(ISilkKeyboard silkKeyboard)
    {
        _silkKeyboard = silkKeyboard;
    }

    public bool IsKeyDown(MyKey key)
    {
        return _keyStates[key] is KeyState.Pressed or KeyState.Held;
    }

    public bool IsKeyPressed(MyKey key)
    {
        return _keyStates[key] == KeyState.Pressed;
    }

    public void Update()
    {
        foreach (var key in _keyStates.Keys)
        {
            var isPressed = _silkKeyboard.IsKeyPressed((SilkKey)key);
            _keyStates[key] = (_keyStates[key], isPressed) switch
            {
                (KeyState.Pressed, true) => KeyState.Held,
                (_, true) => KeyState.Pressed,
                (KeyState.Pressed or KeyState.Held, false) => KeyState.Released,
                (_, false) => KeyState.NotPressed
            };
        }
    }
}

public enum KeyState
{
    Pressed,
    NotPressed,
    Held,
    Released
}

