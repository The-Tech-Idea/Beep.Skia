using System;
using System.Collections.Generic;
using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>
    /// The path a connector takes from its start port to its end port, the one owner that drawing, arrowheads, hit
    /// testing, label placement and bounds all use. Orthogonal routes leave each port away from its component (the side
    /// the port sits on) and arrive the same way: a forward link keeps the familiar three-segment step; a link whose
    /// target lies behind its source goes round through the gap between the two components instead of across them.
    /// </summary>
    public static partial class ConnectionRoute
    {
        /// <summary>How far an orthogonal route runs straight out of a port before it turns.</summary>
        public const float Stub = 16f;
        private const int CurveSegments = 24;

        private enum Side { Left, Right, Top, Bottom }

        /// <summary>The route as a polyline (a curve is flattened). <paramref name="source"/> and
        /// <paramref name="target"/> are the bounds of the components the ports belong to, when known.</summary>
        public static SKPoint[] Points(LineRoutingMode mode, SKPoint start, SKRect? source, SKPoint end, SKRect? target) => mode switch
        {
            LineRoutingMode.Straight => new[] { start, end },
            LineRoutingMode.Orthogonal => Orthogonal(start, source, end, target),
            LineRoutingMode.Curved => Curve(start, end),
        };

        /// <summary>
        /// The route of a sketch line through its points, in order. Two points route exactly as a connector does
        /// (<see cref="Points(LineRoutingMode, SKPoint, SKRect?, SKPoint, SKRect?)"/>). With more: straight joins them; curved passes smoothly through each (Catmull-Rom);
        /// orthogonal turns at right angles between each pair, its first run leaving the start the way a connector leaves
        /// its object (along the longer axis when it has none).
        /// </summary>
        public static SKPoint[] Through(LineRoutingMode mode, IReadOnlyList<SKPoint> points, SKRect? source, SKRect? target)
        {
            if (points == null || points.Count < 2) throw new ArgumentException("A route needs at least two points.", nameof(points));
            if (points.Count == 2) return Points(mode, points[0], source, points[1], target);
            return mode switch
            {
                LineRoutingMode.Straight => System.Linq.Enumerable.ToArray(points),
                LineRoutingMode.Orthogonal => OrthogonalThrough(points, source),
                LineRoutingMode.Curved => CatmullRom(points),
            };
        }

        private const int SpanSegments = 12;
        private static SKPoint[] CatmullRom(IReadOnlyList<SKPoint> points)
        {
            var result = new List<SKPoint>(points.Count * SpanSegments) { points[0] };
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[Math.Max(0, i - 1)]; var p1 = points[i]; var p2 = points[i + 1]; var p3 = points[Math.Min(points.Count - 1, i + 2)];
                for (int s = 1; s <= SpanSegments; s++)
                {
                    float t = s / (float)SpanSegments, t2 = t * t, t3 = t2 * t;
                    result.Add(new SKPoint(
                        0.5f * (2 * p1.X + (p2.X - p0.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (3 * p1.X - p0.X - 3 * p2.X + p3.X) * t3),
                        0.5f * (2 * p1.Y + (p2.Y - p0.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (3 * p1.Y - p0.Y - 3 * p2.Y + p3.Y) * t3)));
                }
            }
            return result.ToArray();
        }

        private static SKPoint[] OrthogonalThrough(IReadOnlyList<SKPoint> points, SKRect? source)
        {
            var first = points[1] - points[0];
            var across = SideOf(points[0], source, Math.Abs(first.X) >= Math.Abs(first.Y) ? Side.Right : Side.Bottom) is Side.Left or Side.Right;
            var result = new List<SKPoint> { points[0] };
            for (int i = 1; i < points.Count; i++)
            {
                var a = result[result.Count - 1]; var b = points[i];
                result.Add(across ? new SKPoint(b.X, a.Y) : new SKPoint(a.X, b.Y));
                result.Add(b);
                across = !across;
            }
            return Simplify(result);
        }

        /// <summary>The control points of the curved route's cubic.</summary>
        public static (SKPoint First, SKPoint Second) CurveControls(SKPoint start, SKPoint end) =>
            (new SKPoint((start.X * 2 + end.X) / 3f, start.Y), new SKPoint((end.X * 2 + start.X) / 3f, end.Y));

        /// <summary>The point halfway along a route, by length.</summary>
        public static SKPoint Middle(IReadOnlyList<SKPoint> points) => At(points, 0.5f);

        /// <summary>The point <paramref name="fraction"/> of the way along a route, by length (0 its start, 1 its end).</summary>
        public static SKPoint At(IReadOnlyList<SKPoint> points, float fraction)
        {
            if (points == null || points.Count == 0) throw new ArgumentException("A route needs at least one point.", nameof(points));
            if (!float.IsFinite(fraction) || fraction < 0 || fraction > 1) throw new ArgumentOutOfRangeException(nameof(fraction), "Choose a fraction from 0 to 1.");
            float total = 0;
            for (int i = 1; i < points.Count; i++) total += SKPoint.Distance(points[i - 1], points[i]);
            var half = total * fraction;
            for (int i = 1; i < points.Count; i++)
            {
                var length = SKPoint.Distance(points[i - 1], points[i]);
                if (length >= half && length > 0)
                {
                    var t = half / length;
                    return new SKPoint(points[i - 1].X + (points[i].X - points[i - 1].X) * t, points[i - 1].Y + (points[i].Y - points[i - 1].Y) * t);
                }
                half -= length;
            }
            return points[points.Count - 1];
        }

        /// <summary>The distance from a point to the nearest segment of a route.</summary>
        public static float Distance(IReadOnlyList<SKPoint> points, SKPoint point)
        {
            var best = float.MaxValue;
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1]; var b = points[i]; var ab = b - a;
                var lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
                var t = lengthSquared == 0 ? 0 : Math.Clamp(((point.X - a.X) * ab.X + (point.Y - a.Y) * ab.Y) / lengthSquared, 0, 1);
                best = Math.Min(best, SKPoint.Distance(point, new SKPoint(a.X + ab.X * t, a.Y + ab.Y * t)));
            }
            return points.Count == 1 ? SKPoint.Distance(point, points[0]) : best;
        }

        private static SKPoint[] Curve(SKPoint start, SKPoint end)
        {
            var (c1, c2) = CurveControls(start, end);
            var points = new SKPoint[CurveSegments + 1];
            for (int i = 0; i <= CurveSegments; i++)
            {
                float t = i / (float)CurveSegments, u = 1 - t;
                points[i] = new SKPoint(
                    u * u * u * start.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * end.X,
                    u * u * u * start.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * end.Y);
            }
            return points;
        }

        private static SKPoint[] Orthogonal(SKPoint start, SKRect? source, SKPoint end, SKRect? target)
        {
            // Without a component to read a side from, the route leaves towards the other end and arrives from it.
            var startSide = SideOf(start, source, end.X >= start.X ? Side.Right : Side.Left);
            var endSide = SideOf(end, target, end.X >= start.X ? Side.Left : Side.Right);
            var leave = Normal(startSide); var arrive = Normal(endSide);
            var a = start + new SKPoint(leave.X * Stub, leave.Y * Stub);
            var b = end + new SKPoint(arrive.X * Stub, arrive.Y * Stub);
            var points = new List<SKPoint> { start, a };
            bool startAcross = startSide is Side.Left or Side.Right, endAcross = endSide is Side.Left or Side.Right;
            if (startAcross && endAcross)
            {
                if (leave.X == -arrive.X && (b.X - a.X) * leave.X >= 0)
                { var midX = (a.X + b.X) / 2; points.Add(new SKPoint(midX, a.Y)); points.Add(new SKPoint(midX, b.Y)); }
                else if (leave.X == arrive.X)
                { var x = leave.X > 0 ? Math.Max(a.X, b.X) : Math.Min(a.X, b.X); points.Add(new SKPoint(x, a.Y)); points.Add(new SKPoint(x, b.Y)); }
                else
                { var y = Channel(a.Y, source?.Top, source?.Bottom, b.Y, target?.Top, target?.Bottom); points.Add(new SKPoint(a.X, y)); points.Add(new SKPoint(b.X, y)); }
            }
            else if (!startAcross && !endAcross)
            {
                if (leave.Y == -arrive.Y && (b.Y - a.Y) * leave.Y >= 0)
                { var midY = (a.Y + b.Y) / 2; points.Add(new SKPoint(a.X, midY)); points.Add(new SKPoint(b.X, midY)); }
                else if (leave.Y == arrive.Y)
                { var y = leave.Y > 0 ? Math.Max(a.Y, b.Y) : Math.Min(a.Y, b.Y); points.Add(new SKPoint(a.X, y)); points.Add(new SKPoint(b.X, y)); }
                else
                { var x = Channel(a.X, source?.Left, source?.Right, b.X, target?.Left, target?.Right); points.Add(new SKPoint(x, a.Y)); points.Add(new SKPoint(x, b.Y)); }
            }
            else if (startAcross)
            {
                // Leave sideways, arrive vertically: one corner, on whichever side keeps both runs heading outwards.
                var corner = new SKPoint(b.X, a.Y);
                bool ahead = (corner.X - a.X) * leave.X >= 0 && (corner.Y - b.Y) * arrive.Y >= 0;
                points.Add(ahead ? corner : new SKPoint(a.X, b.Y));
            }
            else
            {
                var corner = new SKPoint(a.X, b.Y);
                bool ahead = (corner.Y - a.Y) * leave.Y >= 0 && (corner.X - b.X) * arrive.X >= 0;
                points.Add(ahead ? corner : new SKPoint(b.X, a.Y));
            }
            points.Add(b); points.Add(end);
            return Simplify(points);
        }

        /// <summary>A line between two components along one axis, or just beyond both when they overlap on it.</summary>
        private static float Channel(float a, float? aLow, float? aHigh, float b, float? bLow, float? bHigh)
        {
            float sLow = aLow ?? a, sHigh = aHigh ?? a, tLow = bLow ?? b, tHigh = bHigh ?? b;
            if (tLow >= sHigh) return (sHigh + tLow) / 2;
            if (sLow >= tHigh) return (tHigh + sLow) / 2;
            return Math.Max(sHigh, tHigh) + Stub;
        }

        /// <summary>The side of its component a port faces: its offset from the centre, scaled by the half-size, so a port
        /// on a diamond's slanted edge faces the way that edge does.</summary>
        private static Side SideOf(SKPoint port, SKRect? bounds, Side fallback)
        {
            if (bounds is not { } r || r.Width <= 0 || r.Height <= 0) return fallback;
            var across = (port.X - r.MidX) / (r.Width / 2); var down = (port.Y - r.MidY) / (r.Height / 2);
            if (Math.Abs(across) >= Math.Abs(down)) return across >= 0 ? Side.Right : Side.Left;
            return down >= 0 ? Side.Bottom : Side.Top;
        }

        private static SKPoint Normal(Side side) => side switch
        {
            Side.Left => new SKPoint(-1, 0),
            Side.Right => new SKPoint(1, 0),
            Side.Top => new SKPoint(0, -1),
            Side.Bottom => new SKPoint(0, 1),
        };

        /// <summary>Removes repeated points and the middle of three points on one line.</summary>
        private static SKPoint[] Simplify(List<SKPoint> points)
        {
            var result = new List<SKPoint>(points.Count);
            foreach (var p in points)
            {
                if (result.Count > 0 && SKPoint.Distance(result[result.Count - 1], p) < .01f) continue;
                if (result.Count >= 2)
                {
                    var a = result[result.Count - 2]; var b = result[result.Count - 1];
                    if (Math.Abs((b.X - a.X) * (p.Y - b.Y) - (b.Y - a.Y) * (p.X - b.X)) < .01f
                        && (b.X - a.X) * (p.X - b.X) + (b.Y - a.Y) * (p.Y - b.Y) >= 0)
                    { result[result.Count - 1] = p; continue; }
                }
                result.Add(p);
            }
            return result.ToArray();
        }
    }
}
