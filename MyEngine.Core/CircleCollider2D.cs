using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;

namespace MyEngine.Core
{
    public class CircleCollider2D : ICollider2D
    {
        public float Radius { get; set; }

        public CircleCollider2D(float radius)
        {
            Radius = radius;
        }
    }
}
