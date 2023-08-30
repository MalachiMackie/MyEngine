﻿using System.Numerics;
using BepuUtilities;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;
using MathHelper = MyEngine.Utils.MathHelper;

namespace MyEngine.Runtime;

internal class LineRenderResource : ILineRenderResource
{
    private readonly Queue<ILineRenderResource.Line> _lines = new();

    public void RenderLine(Vector3 Start, Vector3 End)
    {
        _lines.Enqueue(new ILineRenderResource.Line(Start, End));
    }

    public IEnumerable<ILineRenderResource.Line> FlushLines()
    {
        while (_lines.TryDequeue(out var line))
        {
            yield return line;
        }
    }

    public Result<Unit, ILineRenderResource.RenderLineCircleError> RenderLineCircle(Vector3 center, float radius, uint? segments = null)
    {
        if (segments is < 3)
        {
            return Result.Failure<Unit, ILineRenderResource.RenderLineCircleError>(ILineRenderResource.RenderLineCircleError.InvalidSegmentCount);
        }

        segments ??= 15;
        var angleDegrees = 360.0f / segments.Value;
        var angleRadians = MathHelper.DegreesToRadians(angleDegrees);

        Vector3? first = null;
        Vector3? previous = null;
        for (int i = 0; i < segments.Value; i++)
        {
            float currentAngle = angleRadians * i;
            var (sin, cos) = MathF.SinCos(currentAngle);
            float x = radius * sin;
            float y = radius * cos;
            float z = 1.0f;

            var point = center + new Vector3(x, y, z);
            if (!first.HasValue)
            {
                first = point;
            }
            if (previous.HasValue)
            {
                _lines.Enqueue(new ILineRenderResource.Line(previous.Value, point));
            }
            previous = point;
        }
        _lines.Enqueue(new ILineRenderResource.Line(previous!.Value, first!.Value));

        return Result.Success<Unit, ILineRenderResource.RenderLineCircleError>(Unit.Value);
    }
}