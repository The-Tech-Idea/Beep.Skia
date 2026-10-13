using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Beep.Skia.Components;

/// <summary>
/// The one owner of arrowhead shapes. Sketch arrows and connectors both draw their heads from it, so an arrowhead chosen in
/// one place looks the same in the other.
/// </summary>
public static class Arrowheads
{
    /// <summary>Where a head sits: its tip, the two corners of its base, and its length.</summary>
    public readonly record struct Frame(SKPoint Tip, SKPoint Left, SKPoint Right, float Size);

    /// <summary>A head <paramref name="size"/> long at <paramref name="tip"/>, pointing away from <paramref name="from"/>;
    /// its base is as wide as it is long.</summary>
    public static Frame At(SKPoint tip, SKPoint from, float size)
    {
        var d = tip - from; var along = d.Length;
        if (along <= 0 || !(size > 0)) return new(tip, tip, tip, 0);
        var ux = d.X / along; var uy = d.Y / along;
        return new(tip, new(tip.X - ux * size - uy * size * .5f, tip.Y - uy * size + ux * size * .5f),
            new(tip.X - ux * size + uy * size * .5f, tip.Y - uy * size - ux * size * .5f), size);
    }

    /// <summary>The head's outlines in world space: open polylines are stroked; filled ones are closed polygons.</summary>
    public static IEnumerable<(SKPoint[] Points, bool Fill)> Shapes(SketchArrowhead head, Frame f)
    {
        if (f.Size == 0) yield break;
        switch (head)
        {
            case SketchArrowhead.None: break;
            case SketchArrowhead.Arrow: yield return (new[] { f.Left, f.Tip, f.Right }, false); break;
            case SketchArrowhead.Triangle: yield return (new[] { f.Left, f.Tip, f.Right, f.Left }, true); break;
            case SketchArrowhead.Bar:
                var half = new SKPoint((f.Left.X - f.Right.X) / 2, (f.Left.Y - f.Right.Y) / 2);
                yield return (new[] { f.Tip + half, f.Tip - half }, false); break;
            case SketchArrowhead.Dot: yield return (Circle(f.Tip, f.Size * 0.35f), true); break;
            case SketchArrowhead.OutlinedTriangle: yield return (new[] { f.Left, f.Tip, f.Right, f.Left }, false); break;
            case SketchArrowhead.OutlinedDot: yield return (Circle(f.Tip, f.Size * 0.35f), false); break;
            case SketchArrowhead.Diamond:
            case SketchArrowhead.OutlinedDiamond:
                // Tip to the base of the head, as wide as a third of the arrowhead's span.
                var back = new SKPoint((f.Left.X + f.Right.X) / 2, (f.Left.Y + f.Right.Y) / 2);
                var middle = new SKPoint((f.Tip.X + back.X) / 2, (f.Tip.Y + back.Y) / 2);
                var side = new SKPoint((f.Left.X - f.Right.X) * 0.3f, (f.Left.Y - f.Right.Y) * 0.3f);
                yield return (new[] { f.Tip, middle + side, back, middle - side, f.Tip }, head == SketchArrowhead.Diamond); break;
            case SketchArrowhead.CrowsFoot:
                // "Many": two prongs from a point on the shaft out to either side of the tip; the shaft is the third.
                var root = new SKPoint((f.Left.X + f.Right.X) / 2, (f.Left.Y + f.Right.Y) / 2);
                var spread = new SKPoint((f.Left.X - f.Right.X) / 2, (f.Left.Y - f.Right.Y) / 2);
                yield return (new[] { f.Tip + spread, root, f.Tip - spread }, false); break;
        }
    }

    /// <summary>How far the shaft stops short of the tip under each head: only outlined heads keep their inside clear.</summary>
    public static float Inset(SketchArrowhead head, Frame f) => head switch
    {
        SketchArrowhead.None or SketchArrowhead.Arrow or SketchArrowhead.Triangle or SketchArrowhead.Dot or SketchArrowhead.Bar
            or SketchArrowhead.Diamond or SketchArrowhead.CrowsFoot => 0,
        SketchArrowhead.OutlinedTriangle or SketchArrowhead.OutlinedDiamond => f.Size,
        SketchArrowhead.OutlinedDot => f.Size * 0.35f,
    };

    private static SKPoint[] Circle(SKPoint center, float radius)
    {
        var circle = new SKPoint[25];
        for (int i = 0; i < circle.Length; i++)
        {
            var angle = i * MathF.PI * 2 / (circle.Length - 1);
            circle[i] = new(center.X + radius * MathF.Cos(angle), center.Y + radius * MathF.Sin(angle));
        }
        return circle;
    }
}
