using Beep.Skia.Model;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// A flowchart shape whose flow can run left to right (inputs down the left side, outputs down the right side) or top to
    /// bottom (inputs along the top, outputs along the bottom). <see cref="ShowTopBottomPorts"/> chooses, and each shape
    /// places its ports on its own outline in both directions: <see cref="LayoutSidePorts"/> and
    /// <see cref="LayoutTopBottomPorts"/>. Ports always run across the flow in index order, so links attached in that order
    /// do not cross at the shape. Shapes whose ports are fixed by their meaning (loops, fork, join, off-page reference,
    /// tape, connector, annotation, comment) are plain <see cref="FlowchartControl"/>s and have no direction.
    /// </summary>
    public abstract class DirectedFlowchartControl : FlowchartControl
    {
        /// <summary>How far in from a shape's corners the top and bottom ports start, in drawing units.</summary>
        protected const float FlowInset = 12f;

        private bool _showTopBottomPorts;

        /// <summary>
        /// Whether the flow runs top to bottom (inputs along the top edge, outputs along the bottom edge) rather than left to
        /// right (inputs down the left side, outputs down the right side).
        /// </summary>
        public bool ShowTopBottomPorts
        {
            get => _showTopBottomPorts;
            set
            {
                if (_showTopBottomPorts == value) return;
                _showTopBottomPorts = value;
                if (NodeProperties.TryGetValue("ShowTopBottomPorts", out var pi))
                    pi.ParameterCurrentValue = value;
                // The ports move: lay them out again and tell connected lines.
                MarkPortsDirty();
                OnBoundsChanged(Bounds);
                InvalidateVisual();
            }
        }

        protected DirectedFlowchartControl()
        {
            NodeProperties["ShowTopBottomPorts"] = new ParameterInfo
            {
                ParameterName = "ShowTopBottomPorts",
                ParameterType = typeof(bool),
                DefaultParameterValue = false,
                ParameterCurrentValue = false,
                Description = "If true, the flow runs top to bottom: inputs along the top edge, outputs along the bottom edge."
            };
        }

        protected sealed override void LayoutPorts()
        {
            if (ShowTopBottomPorts) LayoutTopBottomPorts(); else LayoutSidePorts();
        }

        /// <summary>Places the ports for a flow that runs left to right: inputs on the left, outputs on the right.</summary>
        protected abstract void LayoutSidePorts();

        /// <summary>
        /// Places the ports for a flow that runs top to bottom. The default suits a shape whose top and bottom edges are flat
        /// across its width; a shape with another outline overrides it.
        /// </summary>
        protected virtual void LayoutTopBottomPorts()
        {
            var r = Bounds;
            PlaceAlongEdge(InConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, _ => r.Bottom, +1f);
        }

        /// <summary>
        /// Spreads <paramref name="ports"/> evenly (one sits in the middle) over <paramref name="x0"/> to
        /// <paramref name="x1"/>, each on the outline at <paramref name="edgeY"/> of its x and just outside it: above for
        /// <paramref name="outward"/> -1, below for +1. Ports keep their index in the list.
        /// </summary>
        protected void PlaceAlongEdge(IList<IConnectionPoint> ports, float x0, float x1, Func<float, float> edgeY, float outward)
        {
            if (ports.Count == 0) return;
            if (x1 < x0) x0 = x1 = (x0 + x1) / 2;
            for (int i = 0; i < ports.Count; i++)
            {
                var x = x0 + (x1 - x0) * (i + 1f) / (ports.Count + 1f);
                SetPort(ports[i], new SKPoint(x, edgeY(x) + outward * (PortRadius + 1f)), i);
            }
        }

        /// <summary>
        /// Spreads <paramref name="ports"/> (from index <paramref name="first"/>) down the vertical line at
        /// <paramref name="x"/> between <paramref name="y0"/> and <paramref name="y1"/>, for a side that has no room on its
        /// outline for more than the ports it was drawn with.
        /// </summary>
        protected void PlaceDownLine(IList<IConnectionPoint> ports, int first, float x, float y0, float y1)
        {
            var count = ports.Count - first;
            for (int i = 0; i < count; i++)
                SetPort(ports[first + i], new SKPoint(x, y0 + (y1 - y0) * (i + 1f) / (count + 1f)), first + i);
        }

        /// <summary>
        /// Sets a port the way the fixed-port shapes always have: its centre on the outline at <paramref name="center"/>, the
        /// position connectors attach at a port-radius beyond it (<paramref name="gap"/>, negative on the left).
        /// </summary>
        protected void SetPortBeside(IConnectionPoint cp, SKPoint center, float gap, int index)
        {
            cp.Center = center;
            cp.Position = new SKPoint(center.X + gap, center.Y);
            cp.Bounds = new SKRect(center.X - PortRadius, center.Y - PortRadius, center.X + PortRadius, center.Y + PortRadius);
            cp.Rect = cp.Bounds;
            cp.Index = index;
            cp.Component = this;
        }

        /// <summary>
        /// Stacks <paramref name="ports"/> down the vertical line at <paramref name="x"/>, centred on the shape and
        /// <paramref name="spacing"/> apart, closer when they would not fit: one sits at the middle, so a shape drawn for one
        /// port per side keeps it where it was, and more ports have places too.
        /// </summary>
        protected void PlaceStackBeside(IList<IConnectionPoint> ports, float x, float gap, float spacing)
        {
            var r = Bounds;
            var step = ports.Count > 1 ? Math.Min(spacing, Math.Max(0, r.Height - 20f) / (ports.Count - 1)) : 0f;
            for (int i = 0; i < ports.Count; i++)
                SetPortBeside(ports[i], new SKPoint(x, r.MidY + (i - (ports.Count - 1) / 2f) * step), gap, i);
        }

        /// <summary>The y of a cubic curve at <paramref name="x"/>, for a curve whose x runs one way from end to end.</summary>
        protected static float CubicYAt(SKPoint p0, SKPoint c1, SKPoint c2, SKPoint p3, float x)
        {
            static float Bezier(float a, float b, float c, float d, float t)
            {
                var u = 1 - t;
                return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
            }
            var increasing = p3.X >= p0.X;
            float lo = 0, hi = 1;
            for (int i = 0; i < 40; i++)
            {
                var mid = (lo + hi) / 2;
                if ((Bezier(p0.X, c1.X, c2.X, p3.X, mid) < x) == increasing) lo = mid; else hi = mid;
            }
            return Bezier(p0.Y, c1.Y, c2.Y, p3.Y, (lo + hi) / 2);
        }
    }
}
