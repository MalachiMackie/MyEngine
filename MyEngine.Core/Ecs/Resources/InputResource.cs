using MyEngine.Core.Input;
using System.Numerics;

namespace MyEngine.Core.Ecs.Resources
{
    public class InputResource : IResource
    {
        internal InputResource(MyKeyboard keyboard, MyMouse mouse)
        {
            Keyboard = keyboard;
            Mouse = mouse;
        }

        public MyMouse Mouse { get; internal set; }

        public MyKeyboard Keyboard { get; internal set; }

        public Vector2 MouseDelta { get; internal set; }
    }
}
