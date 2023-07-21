using MyEngine.Core.Input;
using Silk.NET.Input;
using System.Numerics;

namespace MyEngine.Runtime
{
    internal class MyInput
    {
        private readonly IInputContext _context;
        private readonly IKeyboard _primaryKeyboard;

        internal readonly IMouse Mouse;

        public MyInput(MyWindow window)
        {
            _context = window.InnerWindow.CreateInput();

            foreach (var keyboard in _context.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
            }
            Mouse = _context.Mice[0];

            Mouse.MouseMove += OnMouseMove;

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
            return (MyKey)key;
        }


        private static Key MapKey(MyKey key)
        {
            return (Key)key;
        }

        internal bool IsKeyPressed(MyKey key)
        {
            if (key == MyKey.Unknown)
            {
                return false;
            }

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
}
