using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Rendering;

public class RenderStats : IResource
{
    public uint DrawCalls { get; internal set; }
}
