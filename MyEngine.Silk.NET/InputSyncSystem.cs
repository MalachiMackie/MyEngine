using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Input;

namespace MyEngine.Silk.NET;

internal class InputSyncSystem : ISystem
{
    private readonly Keyboard _keyboard;
    private readonly Mouse _mouse;

    public InputSyncSystem(Keyboard keyboard, Mouse mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;
    }


    public void Run(double _)
    {
        _mouse.Update();
        _keyboard.Update();
    }
}
