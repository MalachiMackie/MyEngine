using System.Numerics;

namespace MyEngine
{
    internal class InputResource : IResource
    {
        public InputResource(MyInput input) 
        {
            Input = input;
        }

        public MyInput Input { get; }

        public Vector2 MouseDelta { get; internal set; }
    }
}
