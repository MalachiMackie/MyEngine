﻿using MyEngine.Core.Ecs.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs
{
    public class MyQuery<T>
        where T : IComponent
    {
        public IEnumerable<T>? Components { get; internal set; }
    }

    public class MyQuery<T1, T2> : IEnumerable<(T1, T2)>
        where T1 : IComponent
        where T2 : IComponent
    {
        internal MyQuery(Func<IEnumerable<(T1, T2)>> components)
        {
            ComponentsFunc = components;
        }

        internal Func<IEnumerable<(T1, T2)>> ComponentsFunc { get; set; }

        public IEnumerator<(T1, T2)> GetEnumerator()
        {
            return ComponentsFunc().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}