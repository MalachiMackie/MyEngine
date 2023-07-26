using MyEngine.Core.Ecs.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs
{
    public class MyQuery<T> : IEnumerable<T>
        where T : IComponent
    {
        internal MyQuery(Func<IEnumerable<T>> components)
        {
            ComponentsFunc = components;
        }

        internal Func<IEnumerable<T>> ComponentsFunc { get; set; }

        public IEnumerator<T> GetEnumerator()
        {
            return ComponentsFunc().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
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

    public class MyQuery<T1, T2, T3> : IEnumerable<(T1, T2, T3)>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        internal MyQuery(Func<IEnumerable<(T1, T2, T3)>> components)
        {
            ComponentsFunc = components;
        }

        internal Func<IEnumerable<(T1, T2, T3)>> ComponentsFunc { get; set; }

        public IEnumerator<(T1, T2, T3)> GetEnumerator()
        {
            return ComponentsFunc().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
