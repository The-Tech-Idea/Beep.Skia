using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Beep.Skia;

/// <summary>How a line shows movement along it: its own dashes flowing, or dots travelling over it.</summary>
public enum LineFlow { None, Dashes, Dots }
/// <summary>How fast a line's flow moves. Each speed moves a whole number of patterns a second, so every flow on a board
/// repeats exactly once a second and an animation of one second loops without a jump.</summary>
public enum LineFlowSpeed { Slow, Normal, Fast }
/// <summary>Which way a flow moves along its line: from the start to the end, or from the end back to the start.</summary>
public enum LineFlowDirection { Forward, Backward }

/// <summary>
/// The one owner of how a flowing line looks at a moment: the patterns, their sizes and where they are along the line.
/// Sketch lines and connectors both paint their flow through it, and every export draws the same moment the same way.
/// The pattern moves by changing the dash phase along the drawn path, so it follows straight, elbow and curved routes.
/// </summary>
public static class FlowAnimation
{
    /// <summary>Every flow repeats after this many seconds, whatever its speed.</summary>
    public const double LoopSeconds = 1;

    /// <summary>Patterns moved per second; whole numbers, so each flow is back where it started after a second.</summary>
    public static int PatternsPerSecond(LineFlowSpeed speed) => speed switch
    {
        LineFlowSpeed.Slow => 1,
        LineFlowSpeed.Normal => 2,
        LineFlowSpeed.Fast => 4,
    };

    /// <summary>A dashed line's pattern at a stroke width (the same pattern a still dashed sketch line draws).</summary>
    public static float[] DashPattern(float width) => new[] { width * 4 + 4, width * 3 + 4 };
    /// <summary>A dotted line's pattern: zero-length dashes with round caps draw as dots.</summary>
    public static float[] DotPattern(float width) => new[] { 0f, width * 2 + 4 };
    /// <summary>The diameter of the dots that travel over a line of this width.</summary>
    public static float TravellingDotSize(float width) => width * 2 + 3;
    /// <summary>The spacing of travelling dots: a zero-length dash every so far, drawn with a round cap.</summary>
    public static float[] TravellingDotPattern(float width) => new[] { 0f, width * 6 + 18 };

    /// <summary>
    /// The dash phase that places <paramref name="pattern"/> where it is at <paramref name="seconds"/>. Forward moves the
    /// pattern from the start of the path toward its end: a larger phase starts further into the pattern, which moves
    /// it back toward the start, so forward subtracts the distance travelled.
    /// </summary>
    public static float Phase(IReadOnlyList<float> pattern, LineFlowSpeed speed, LineFlowDirection direction, double seconds)
    {
        if (pattern == null || pattern.Count < 2) throw new ArgumentException("A flow needs a dash pattern.", nameof(pattern));
        if (!double.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), "The animation time must be a finite number.");
        float period = 0;
        foreach (var interval in pattern) period += interval;
        var turns = seconds * PatternsPerSecond(speed);
        var travelled = (float)(turns - Math.Floor(turns)) * period;
        return direction switch
        {
            LineFlowDirection.Forward => travelled == 0 ? 0 : period - travelled,
            LineFlowDirection.Backward => travelled,
        };
    }

    /// <summary>A dash effect for the moving pattern; the caller disposes it.</summary>
    public static SKPathEffect Effect(float[] pattern, LineFlowSpeed speed, LineFlowDirection direction, double seconds) =>
        SKPathEffect.CreateDash(pattern, Phase(pattern, speed, direction, seconds));

    /// <summary>Draws the dots that travel over <paramref name="path"/>, in the line's colour.</summary>
    public static void DrawTravellingDots(SKCanvas canvas, SKPath path, SKColor color, float width,
        LineFlowSpeed speed, LineFlowDirection direction, double seconds)
    {
        if (canvas == null || path == null || path.IsEmpty) return;
        var pattern = TravellingDotPattern(width);
        using var effect = Effect(pattern, speed, direction, seconds);
        using var paint = new SKPaint
        {
            IsAntialias = true, Color = color, Style = SKPaintStyle.Stroke, StrokeWidth = TravellingDotSize(width),
            StrokeCap = SKStrokeCap.Round, PathEffect = effect,
        };
        canvas.DrawPath(path, paint);
    }

    /// <summary>How far travelling dots reach beyond the line's centre, for ink bounds.</summary>
    public static float Reach(LineFlow flow, float width) => flow switch
    {
        LineFlow.None or LineFlow.Dashes => width / 2,
        LineFlow.Dots => Math.Max(width, TravellingDotSize(width)) / 2,
    };
}
