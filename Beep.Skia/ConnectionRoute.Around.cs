using System;
using System.Collections.Generic;
using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia
{
    public static partial class ConnectionRoute
    {
        /// <summary>A detour costs this much more for each corner, so it prefers few corners to a slightly shorter path.</summary>
        private const float CornerCost = 3 * RouteObstacles.Margin + 4;
        /// <summary>A detour is searched among at most this many nearby objects; beyond that the ordinary route is kept.</summary>
        private const int MostObstacles = 120;

        /// <summary>
        /// The route of a connector or an attached two-point line on a page with other objects. An elbow route that would
        /// pass through an object other than the two it joins goes round it instead; when nothing is in the way (or no clear
        /// route exists) it is exactly <see cref="Points(LineRoutingMode, SKPoint, SKRect?, SKPoint, SKRect?)"/>, so lines
        /// that cross nothing look as they always did.
        /// </summary>
        public static SKPoint[] Points(LineRoutingMode mode, SKPoint start, SKRect? source, SKPoint end, SKRect? target,
            RouteObstacles obstacles, Guid sourceId, Guid targetId)
        {
            var plain = Points(mode, start, source, end, target);
            if (mode != LineRoutingMode.Orthogonal || obstacles == null || obstacles.Count == 0) return plain;
            var blocked = false;
            for (int i = 1; i < plain.Length && !blocked; i++) blocked = obstacles.Blocks(plain[i - 1], plain[i], sourceId, targetId);
            return blocked ? Around(start, source, end, target, obstacles, sourceId, targetId) ?? plain : plain;
        }

        /// <summary>A shortest elbow path with few corners between the two stubs, over a grid made of the nearby objects'
        /// widened edges; null when there is none within reach.</summary>
        private static SKPoint[] Around(SKPoint start, SKRect? source, SKPoint end, SKRect? target, RouteObstacles obstacles, Guid sourceId, Guid targetId)
        {
            var startSide = SideOf(start, source, end.X >= start.X ? Side.Right : Side.Left);
            var endSide = SideOf(end, target, end.X >= start.X ? Side.Left : Side.Right);
            var leave = Normal(startSide); var arrive = Normal(endSide);
            var stubA = start + new SKPoint(leave.X * Stub, leave.Y * Stub);
            var stubB = end + new SKPoint(arrive.X * Stub, arrive.Y * Stub);
            // The two joined objects are avoided too, beyond their own stubs.
            SKRect? Widened(SKRect? r) { if (r is not { } v) return null; v.Inflate(RouteObstacles.Margin, RouteObstacles.Margin); return v; }
            var sourceArea = Widened(source); var targetArea = Widened(target);
            var a = Exit(stubA, leave, sourceArea); var b = Exit(stubB, arrive, targetArea);

            var span = new SKRect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            var reach = Math.Max(160, Math.Max(span.Width, span.Height) / 2);
            var area = span; area.Inflate(reach, reach);
            var rects = obstacles.Within(area, sourceId, targetId);
            if (rects.Count > MostObstacles) return null;
            if (sourceArea is { } s) rects.Add(s);
            if (targetArea is { } t && (sourceArea is not { } same || same != t)) rects.Add(t);
            foreach (var r in rects) if (StrictlyInside(a, r) || StrictlyInside(b, r)) return null;

            var xs = Coordinates(a.X, b.X, area.Left, area.Right, rects, horizontal: true);
            var ys = Coordinates(a.Y, b.Y, area.Top, area.Bottom, rects, horizontal: false);
            int nx = xs.Length, ny = ys.Length;
            var blockedNode = new bool[nx * ny]; var blockedAcross = new bool[nx * ny]; var blockedDown = new bool[nx * ny];
            foreach (var r in rects)
            {
                // Grid lines on a rectangle's edges stay usable; only its inside is closed.
                int x0 = Index(xs, r.Left), x1 = Index(xs, r.Right), y0 = Index(ys, r.Top), y1 = Index(ys, r.Bottom);
                for (int j = y0; j <= y1; j++)
                    for (int i = x0; i <= x1; i++)
                    {
                        bool insideX = xs[i] > r.Left && xs[i] < r.Right, insideY = ys[j] > r.Top && ys[j] < r.Bottom;
                        if (insideX && insideY) blockedNode[j * nx + i] = true;
                        if (insideY && i < x1) blockedAcross[j * nx + i] = true;   // from (i, j) to (i + 1, j)
                        if (insideX && j < y1) blockedDown[j * nx + i] = true;     // from (i, j) to (i, j + 1)
                    }
            }

            int ia = Array.BinarySearch(xs, a.X), ja = Array.BinarySearch(ys, a.Y);
            int ib = Array.BinarySearch(xs, b.X), jb = Array.BinarySearch(ys, b.Y);
            int startDir = Direction(leave), finalDir = Direction(new SKPoint(-arrive.X, -arrive.Y));
            var cost = new float[nx * ny * 4]; var previous = new int[nx * ny * 4];
            Array.Fill(cost, float.MaxValue); Array.Fill(previous, -1);
            var queue = new PriorityQueue<int, float>();
            int first = (ja * nx + ia) * 4 + startDir;
            cost[first] = 0; queue.Enqueue(first, Estimate(xs[ia], ys[ja], b));
            int goal = -1; float best = float.MaxValue;
            while (queue.TryDequeue(out var state, out var priority))
            {
                if (priority >= best) break;
                int node = state / 4, dir = state % 4, i = node % nx, j = node / nx;
                var here = cost[state];
                if (i == ib && j == jb)
                {
                    var total = here + Turns(dir, finalDir) * CornerCost;
                    if (total < best) { best = total; goal = state; }
                    continue;
                }
                for (int next = 0; next < 4; next++)
                {
                    if (next == Opposite(dir)) continue;
                    int ni = i + (next == 0 ? 1 : next == 1 ? -1 : 0), nj = j + (next == 2 ? 1 : next == 3 ? -1 : 0);
                    if (ni < 0 || nj < 0 || ni >= nx || nj >= ny || blockedNode[nj * nx + ni]) continue;
                    var closed = next switch
                    {
                        0 => blockedAcross[j * nx + i],
                        1 => blockedAcross[j * nx + ni],
                        2 => blockedDown[j * nx + i],
                        _ => blockedDown[nj * nx + i],
                    };
                    if (closed) continue;
                    var step = Math.Abs(xs[ni] - xs[i]) + Math.Abs(ys[nj] - ys[j]) + (next == dir ? 0 : CornerCost);
                    var nextState = (nj * nx + ni) * 4 + next;
                    if (here + step >= cost[nextState]) continue;
                    cost[nextState] = here + step; previous[nextState] = state;
                    queue.Enqueue(nextState, here + step + Estimate(xs[ni], ys[nj], b));
                }
            }
            if (goal < 0) return null;
            var path = new List<SKPoint>();
            for (var state = goal; state >= 0; state = previous[state]) { var node = state / 4; path.Add(new SKPoint(xs[node % nx], ys[node / nx])); }
            path.Reverse();
            var points = new List<SKPoint> { start, stubA };
            points.AddRange(path);
            points.Add(stubB); points.Add(end);
            return Simplify(points);
        }

        /// <summary>The grid lines: both ends, the middle between them, the area's edges and every widened object edge.</summary>
        private static float[] Coordinates(float a, float b, float low, float high, List<SKRect> rects, bool horizontal)
        {
            var set = new SortedSet<float> { a, b, (a + b) / 2, low, high };
            foreach (var r in rects) { set.Add(horizontal ? r.Left : r.Top); set.Add(horizontal ? r.Right : r.Bottom); }
            var result = new float[set.Count]; set.CopyTo(result); return result;
        }
        /// <summary>The index of the first grid line at or after <paramref name="value"/>, or the last line.</summary>
        private static int Index(float[] lines, float value)
        {
            var i = Array.BinarySearch(lines, value);
            return i >= 0 ? i : Math.Min(~i, lines.Length - 1);
        }
        private static bool StrictlyInside(SKPoint p, SKRect r) => p.X > r.Left && p.X < r.Right && p.Y > r.Top && p.Y < r.Bottom;
        /// <summary>A stub end that lies inside its own widened object moves on along the stub until it is outside.</summary>
        private static SKPoint Exit(SKPoint p, SKPoint direction, SKRect? area)
        {
            if (area is not { } r || !StrictlyInside(p, r)) return p;
            if (direction.X > 0) return new SKPoint(r.Right, p.Y);
            if (direction.X < 0) return new SKPoint(r.Left, p.Y);
            if (direction.Y > 0) return new SKPoint(p.X, r.Bottom);
            return new SKPoint(p.X, r.Top);
        }
        private static float Estimate(float x, float y, SKPoint goal) => Math.Abs(goal.X - x) + Math.Abs(goal.Y - y);
        /// <summary>0 right, 1 left, 2 down, 3 up.</summary>
        private static int Direction(SKPoint unit) => unit.X > 0 ? 0 : unit.X < 0 ? 1 : unit.Y > 0 ? 2 : 3;
        private static int Opposite(int dir) => dir ^ 1;
        private static int Turns(int from, int to) => from == to ? 0 : from == Opposite(to) ? 2 : 1;
    }
}
