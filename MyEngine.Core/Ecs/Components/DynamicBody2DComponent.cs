﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine.Core.Ecs.Components
{
    public class DynamicBody2DComponent : IComponent
    {
        public EntityId EntityId { get; }

        public DynamicBody2DComponent(EntityId entityId)
        {
            EntityId = entityId;
        }
    }
}