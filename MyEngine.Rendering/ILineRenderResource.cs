using System.Numerics;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Rendering;

public interface ILineRenderResource : IResource
{
    public void RenderLine(Vector3 start, Vector3 end);

    public Result<Unit> RenderLineCircle(Vector3 center, float radius, uint? segmentCount = null);

    internal readonly record struct Line(Vector3 Start, Vector3 End);
    internal IEnumerable<Line> Flush();
}
