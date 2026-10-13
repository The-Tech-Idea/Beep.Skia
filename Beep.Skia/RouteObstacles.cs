using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Beep.Skia;

/// <summary>
/// The objects on one page that elbow routes go round: a snapshot of their bounds with a bucket index. The host builds a
/// new snapshot when the page changes and gives the same one to every line, so each line works out its route once per
/// change (<see cref="ConnectionRoute.Points(Model.LineRoutingMode, SKPoint, SKRect?, SKPoint, SKRect?, RouteObstacles, Guid, Guid)"/>).
/// </summary>
public sealed class RouteObstacles
{
    /// <summary>How far a detour keeps from the objects it goes round, in drawing units.</summary>
    public const float Margin = 12;
    private const float Cell = 256;
    private readonly (Guid Owner, SKRect Bounds)[] _items;
    private readonly Dictionary<(int X, int Y), List<int>> _cells = new();

    /// <param name="items">Each object's identity and bounds in world coordinates. Empty or non-finite bounds are left out.</param>
    public RouteObstacles(IEnumerable<(Guid Owner, SKRect Bounds)> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        var kept = new List<(Guid, SKRect)>();
        foreach (var (owner, bounds) in items)
            if (bounds.Width > 0 && bounds.Height > 0 && float.IsFinite(bounds.Left) && float.IsFinite(bounds.Top)
                && float.IsFinite(bounds.Right) && float.IsFinite(bounds.Bottom)) kept.Add((owner, bounds));
        _items = kept.ToArray();
        for (int i = 0; i < _items.Length; i++)
        {
            var r = _items[i].Bounds; r.Inflate(Margin, Margin);
            foreach (var cell in CellsOf(r))
            {
                if (!_cells.TryGetValue(cell, out var list)) _cells[cell] = list = new List<int>();
                list.Add(i);
            }
        }
    }

    /// <summary>How many objects routes go round.</summary>
    public int Count => _items.Length;

    private static IEnumerable<(int, int)> CellsOf(SKRect r)
    {
        int x0 = (int)Math.Floor(r.Left / Cell), x1 = (int)Math.Floor(r.Right / Cell);
        int y0 = (int)Math.Floor(r.Top / Cell), y1 = (int)Math.Floor(r.Bottom / Cell);
        for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) yield return (x, y);
    }

    private HashSet<int> Candidates(SKRect area)
    {
        var found = new HashSet<int>();
        foreach (var cell in CellsOf(area))
            if (_cells.TryGetValue(cell, out var list)) found.UnionWith(list);
        return found;
    }

    /// <summary>Whether the segment from <paramref name="a"/> to <paramref name="b"/> passes through an object other than
    /// the two a line joins.</summary>
    internal bool Blocks(SKPoint a, SKPoint b, Guid skipA, Guid skipB)
    {
        var area = new SKRect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        foreach (var i in Candidates(area))
        {
            var (owner, bounds) = _items[i];
            if (owner == skipA || owner == skipB) continue;
            // Strictly through the inside: a route running along an edge does not count.
            var inside = bounds; inside.Inflate(-1, -1);
            if (inside.Width > 0 && inside.Height > 0 && SegmentCrosses(a, b, inside)) return true;
        }
        return false;
    }

    /// <summary>The bounds, widened by <see cref="Margin"/>, of the objects that touch <paramref name="area"/>, other than
    /// the two a line joins.</summary>
    internal List<SKRect> Within(SKRect area, Guid skipA, Guid skipB)
    {
        var result = new List<SKRect>();
        foreach (var i in Candidates(area))
        {
            var (owner, bounds) = _items[i];
            if (owner == skipA || owner == skipB) continue;
            var r = bounds; r.Inflate(Margin, Margin);
            if (r.IntersectsWith(area)) result.Add(r);
        }
        return result;
    }

    /// <summary>Liang–Barsky: whether a segment enters the rectangle's inside.</summary>
    private static bool SegmentCrosses(SKPoint a, SKPoint b, SKRect r)
    {
        float enter = 0, exit = 1; var d = b - a;
        bool Clip(float p, float q)
        {
            if (p == 0) return q > 0;
            var t = q / p;
            if (p < 0) { if (t > exit) return false; if (t > enter) enter = t; }
            else { if (t < enter) return false; if (t < exit) exit = t; }
            return true;
        }
        return Clip(-d.X, a.X - r.Left) && Clip(d.X, r.Right - a.X) && Clip(-d.Y, a.Y - r.Top) && Clip(d.Y, r.Bottom - a.Y) && exit > enter;
    }
}
