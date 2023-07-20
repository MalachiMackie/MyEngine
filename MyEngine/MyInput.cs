using Silk.NET.Input;
using System.Numerics;

namespace MyEngine
{
    internal class MyInput
    {
        private readonly IInputContext _context;
        private readonly IKeyboard _primaryKeyboard;

        public MyInput(MyWindow window)
        {
            _context = window.InnerWindow.CreateInput();

            foreach (var keyboard in _context.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
            }

            foreach (var mouse in _context.Mice)
            {
                mouse.MouseMove += OnMouseMove;
            }

            _primaryKeyboard = _context.Keyboards[0];
        }

        private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
        {
            KeyDown?.Invoke(this, new KeyDownEvent
            {
                Key = MapKey(key),
                KeyCode = keyCode
            });
        }

        private void OnMouseMove(IMouse mouse, Vector2 position)
        {
            MouseMove?.Invoke(this, new MouseMoveEvent
            {
                Position = position,
            });
        }

        private static MyKey MapKey(Key key)
        {
            return key switch
            {
                Key.Escape => MyKey.Escape,
                Key.W => MyKey.W,
                Key.A => MyKey.A,
                Key.S => MyKey.S,
                Key.D => MyKey.D,
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            };
        }

        private static Key MapKey(MyKey key)
        {
            return key switch
            {
                MyKey.Escape => Key.Escape,
                MyKey.W => Key.W,
                MyKey.A => Key.A,
                MyKey.S => Key.S,
                MyKey.D => Key.D,
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            };
        }

        internal bool IsKeyPressed(MyKey key)
        {
            return _primaryKeyboard.IsKeyPressed(MapKey(key));
        }

        public event EventHandler<KeyDownEvent>? KeyDown;
        public event EventHandler<MouseMoveEvent>? MouseMove;

        public struct KeyDownEvent
        { 
            public MyKey Key { get; set; }

            public int KeyCode { get; set; }
        }

        public struct MouseMoveEvent
        {
            public Vector2 Position { get; set; }
        }
    }


    public enum MyKey
    {
        Escape,
        W,
        A,
        S,
        D
    }
}
