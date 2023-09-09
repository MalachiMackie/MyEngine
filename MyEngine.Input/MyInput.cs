﻿using MyEngine.Core.Ecs.Resources;
using Silk.NET.Input;
using System.Numerics;

namespace MyEngine.Input;

public class MyInput : IResource
{
    private IInputContext? _context;
    private IKeyboard? _primaryKeyboard;
    internal IMouse? Mouse;

    public MyInput()
    {
    }

    public void Initialize(IInputContext context)
    {
        _context = context;

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

        return _primaryKeyboard?.IsKeyPressed(MapKey(key)) ?? false;
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