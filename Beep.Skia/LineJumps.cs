using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Beep.Skia;

/// <summary>How a line shows that it crosses a line drawn before it: not at all, with a small arc over it, or with a gap.</summary>
public enum LineJumpStyle { None, Arc, Gap }

/// <summary>
/// The one owner of line jumps. <see cref="Find"/> works out, for lines in drawing order, where each crosses a line drawn
/// before it; <see cref="Build"/> draws a route with those crossings as arcs or gaps. Sketch lines and connectors both use it,
/// so a crossing looks the same on screen and in every export. Jumps are derived from the routes, never stored.
/// </summary>
public static class LineJumps
{
    /// <summary>Half the width of a jump, in drawing units, for a line of this stroke width.</summary>
    public static float Size(float strokeWidth) => Math.Max(5, strokeWidth * 2 + 2);
    private const float Cell = 128;

    /// <summary>One line's route and whether it draws jumps: curves are jumped over but draw none themselves.</summary>
    public readonly record struct Line(IReadOnlyList<SKPoint> Route, bool Jumps, float StrokeWidth);

    /// <summary>For each line, in the order given (drawing order), the distances along its route where it crosses a line
    /// drawn before it. Crossings too near a corner or an end, or too near each other, are left out so jumps never overlap.</summary>
    public static float[][] Find(IReadOnlyList<Line> lines)
    {
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        var result = new float[lines.Count][];
        var cells = new Dictionary<(int, int), List<(SKPoint A, SKPoint B)>>();
        for (int k = 0; k < lines.Count; k++)
        {
            var route = lines[k].Route;
            var found = new List<float>();
            if (lines[k].Jumps && route != null && route.Count >= 2)
            {
                var size = Size(lines[k].StrokeWidth); float walked = 0;
                for (int s = 1; s < route.Count; s++)
                {
                    var a = route[s - 1]; var b = route[s]; var length = SKPoint.Distance(a, b);
                    var crossings = new List<float>();
                    foreach (var cell in CellsOf(a, b))
                        if (cells.TryGetValue(cell, out var segments))
                            foreach (var (c, d) in segments)
                                if (Crossing(a, b, c, d) is { } t && t * length > size * 1.5f && (1 - t) * length > size * 1.5f) crossings.Add(t * length);
                    crossings.Sort();
                    foreach (var at in crossings)
                    {
                        var distance = walked + at;
                        if (found.Count == 0 || distance - found[found.Count - 1] > size * 2 + 2) found.Add(distance);
                    }
                    walked += length;
                }
            }
            result[k] = found.ToArray();
            if (route == null) continue;
            for (int s = 1; s < route.Count; s++)
                foreach (var cell in CellsOf(route[s - 1], route[s]))
                {
                    if (!cells.TryGetValue(cell, out var list)) cells[cell] = list = new List<(SKPoint, SKPoint)>();
                    list.Add((route[s - 1], route[s]));
                }
        }
        return result;
    }

    /// <summary>The route as a path with a jump at each distance in <paramref name="at"/> (ascending): a half-circle arc on
    /// the upper (or, for an upright segment, left) side, or a gap.</summary>
    public static void Build(SKPathBuilder builder, IReadOnlyList<SKPoint> route, IReadOnlyList<float> at, LineJumpStyle style, float strokeWidth)
    {
        if (builder == null || route == null || route.Count < 2) return;
        builder.MoveTo(route[0]);
        var size = Size(strokeWidth); int next = 0; float walked = 0;
        for (int s = 1; s < route.Count; s++)
        {
            var a = route[s - 1]; var b = route[s]; var length = SKPoint.Distance(a, b);
            if (length > 0 && style != LineJumpStyle.None && at != null)
            {
                var u = new SKPoint((b.X - a.X) / length, (b.Y - a.Y) / length);
                // The side the arc rises to: up, or left for an upright segment, whichever way the segment runs.
                var n = new SKPoint(u.Y, -u.X);
                if (n.Y > 0.01f || Math.Abs(n.Y) <= 0.01f && n.X > 0) n = new SKPoint(-n.X, -n.Y);
                while (next < at.Count && at[next] <= walked + length)
                {
                    var p = a + new SKPoint(u.X * (at[next] - walked), u.Y * (at[next] - walked));
                    var before = p - new SKPoint(u.X * size, u.Y * size); var after = p + new SKPoint(u.X * size, u.Y * size);
                    builder.LineTo(before);
                    switch (style)
                    {
                        case LineJumpStyle.Arc:
                            // A half circle as one cubic: control points 4/3 of the radius out.
                            var lift = new SKPoint(n.X * size * 4 / 3, n.Y * size * 4 / 3);
                            builder.CubicTo(before + lift, after + lift, after);
                            break;
                        case LineJumpStyle.Gap:
                            builder.MoveTo(after);
                            break;
                        case LineJumpStyle.None:
                            break;
                    }
                    next++;
                }
            }
            builder.LineTo(b);
            walked += length;
        }
    }

    /// <summary>A short, order-independent signature of a line's jumps, for render caches.</summary>
    public static int Signature(LineJumpStyle style, IReadOnlyList<float> at)
    {
        if (style == LineJumpStyle.None || at == null || at.Count == 0) return 0;
        var hash = new HashCode(); hash.Add(style);
        foreach (var d in at) hash.Add(d);
        return hash.ToHashCode();
    }

    /// <summary>Where along a–b (0 to 1) it properly crosses c–d; null when they do not cross or run side by side.</summary>
    private static float? Crossing(SKPoint a, SKPoint b, SKPoint c, SKPoint d)
    {
        var r = b - a; var s = d - c; var denominator = r.X * s.Y - r.Y * s.X;
        if (Math.Abs(denominator) < 1e-6f) return null;
        var t = ((c.X - a.X) * s.Y - (c.Y - a.Y) * s.X) / denominator;
        var u = ((c.X - a.X) * r.Y - (c.Y - a.Y) * r.X) / denominator;
        return t > 0 && t < 1 && u > 0 && u < 1 ? t : null;
    }

    private static IEnumerable<(int, int)> CellsOf(SKPoint a, SKPoint b)
    {
        int x0 = (int)Math.Floor(Math.Min(a.X, b.X) / Cell), x1 = (int)Math.Floor(Math.Max(a.X, b.X) / Cell);
        int y0 = (int)Math.Floor(Math.Min(a.Y, b.Y) / Cell), y1 = (int)Math.Floor(Math.Max(a.Y, b.Y) / Cell);
        for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) yield return (x, y);
    }
}
