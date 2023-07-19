using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine
{
    internal class Entity
    {
        public Entity()
        {
            Id = EntityId.Generate();
        }

        public Entity(EntityId id)
        {
            Id = id;
        }

        public EntityId Id { get; }
    }
}
