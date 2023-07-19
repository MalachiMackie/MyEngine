using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine
{
    internal interface IComponent
    {
        public EntityId EntityId { get; }

        public static abstract bool AllowMultiple { get; } 
    }
}
