using System.Numerics;
using MyEngine.Utils;
using static MyEngine.Rendering.ILineRenderResource;

namespace MyEngine.Rendering;

internal class LineRenderResource : ILineRenderResource
{
    private readonly Queue<Line> _lines = new();

    public void RenderLine(Vector3 Start, Vector3 End)
    {
        _lines.Enqueue(new Line(Start, End));
    }

    public IEnumerable<Line> Flush()
    {
        while (_lines.TryDequeue(out var line))
        {
            yield return line;
        }
    }

    public Result<Unit> RenderLineCircle(Vector3 center, float radius, uint? segments = null)
    {
        if (segments is < 3)
        {
            return Result.Failure<Unit>("Cannot have less than 3 segments for a circle line render");
        }

        segments ??= 15;
        var angleDegrees = 360.0f / segments.Value;
        var angleRadians = MathHelper.DegreesToRadians(angleDegrees);

        Vector3? first = null;
        Vector3? previous = null;
        for (var i = 0; i < segments.Value; i++)
        {
            var currentAngle = angleRadians * i;
            var (sin, cos) = MathF.SinCos(currentAngle);
            var x = radius * sin;
            var y = radius * cos;
            var z = 1.0f;

            var point = center + new Vector3(x, y, z);
            if (!first.HasValue)
            {
                first = point;
            }
            if (previous.HasValue)
            {
                _lines.Enqueue(new Line(previous.Value, point));
            }
            previous = point;
        }
        _lines.Enqueue(new Line(previous!.Value, first!.Value));

        return Result.Success(Unit.Value);
    }
}
