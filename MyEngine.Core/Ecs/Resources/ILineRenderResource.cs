using MyEngine.Utils;

namespace MyEngine.Core.Ecs.Resources;

public interface ILineRenderResource : IResource
{
    public void RenderLine(Vector3 start, Vector3 end);

    public enum RenderLineCircleError
    {
        InvalidSegmentCount
    }
    public Result<Unit, RenderLineCircleError> RenderLineCircle(Vector3 center, float radius, uint? segmentCount = null);

    internal readonly record struct Line(Vector3 Start, Vector3 End);
    internal IEnumerable<Line> FlushLines();
}
