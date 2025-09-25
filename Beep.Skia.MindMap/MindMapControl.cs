using SkiaSharp;
using System;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.MindMap
{
    /// <summary>
    /// Base control for Mind Map nodes providing Material theming and standardized port management.
    /// </summary>
    public abstract class MindMapControl : MaterialControl
    {
        protected const float PortRadius = 4f;

        /// <summary>
        /// Default background color for Mind Map nodes.
        /// </summary>
        public SKColor BackgroundColor { get; set; } = MaterialColors.Surface;

        /// <summary>
        /// Default border color for Mind Map nodes.
        /// </summary>
        public SKColor BorderColor { get; set; } = MaterialColors.Outline;

        /// <summary>
        /// Border thickness.
        /// </summary>
        public float BorderThickness { get; set; } = 2.0f;

        protected MindMapControl()
        {
            TextColor = MaterialColors.OnSurface;
            Width = 140;
            Height = 56;
        }

        /// <summary>
        /// Number of input and output ports (exposed for persistence/editors).
        /// Setting these will resize the underlying port collections and relayout.
        /// </summary>
        public int InPortCount
        {
            get => InConnectionPoints.Count;
            set => EnsurePortCounts(Math.Max(0, value), OutConnectionPoints.Count);
        }

        public int OutPortCount
        {
            get => OutConnectionPoints.Count;
            set => EnsurePortCounts(InConnectionPoints.Count, Math.Max(0, value));
        }

        /// <summary>
        /// Ensure the exact number of input and output ports, then layout.
        /// </summary>
        protected void EnsurePortCounts(int inputs, int outputs)
        {
            while (InConnectionPoints.Count < inputs)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inputs)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outputs)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outputs)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            LayoutPorts();
        }

        /// <summary>
        /// Default port layout: one input on the left middle; all outputs vertically on the right.
        /// </summary>
        protected virtual void LayoutPorts()
        {
            LayoutPortsRightEdge(topInset: 6f, bottomInset: 6f);
        }

        /// <summary>
        /// Layout ports with one input on the left center and all outputs distributed vertically on the right edge.
        /// Lets callers provide insets to avoid special shape areas (e.g., dog-ear fold).
        /// </summary>
        /// <param name="topInset">Top padding within the bounds when distributing outputs.</param>
        /// <param name="bottomInset">Bottom padding within the bounds when distributing outputs.</param>
        protected void LayoutPortsRightEdge(float topInset = 6f, float bottomInset = 6f)
        {
            var b = Bounds;

            // Input on left middle (if present)
            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                float cx = b.Left - 2f;
                float cy = b.MidY;
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            // Outputs along right edge between insets
            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            float yTop = Bounds.Top + Math.Max(0, topInset);
            float yBottom = Bounds.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = Bounds.Right + 2f;
                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        /// <summary>
        /// Layout inputs on the left and outputs around an ellipse perimeter (for central nodes).
        /// </summary>
        protected void LayoutOutputsOnEllipse(float topInset = 4f, float bottomInset = 4f, float outwardOffset = 2f)
        {
            var b = Bounds;
            float cx = b.MidX;
            float cy = b.MidY;
            float rx = Math.Max(1f, b.Width / 2f);
            float ry = Math.Max(1f, b.Height / 2f);

            // single input (if any) on left middle
            if (InConnectionPoints.Count > 0)
            {
                var inp = InConnectionPoints[0];
                float ix = b.Left - 2f;
                float iy = cy;
                inp.Center = new SKPoint(ix, iy);
                inp.Position = inp.Center;
                inp.Bounds = new SKRect(ix - PortRadius, iy - PortRadius, ix + PortRadius, iy + PortRadius);
                inp.Rect = inp.Bounds;
                inp.Index = 0;
                inp.Component = this;
                inp.IsAvailable = true;
            }

            int n = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                // distribute across vertical span and project to ellipse right side
                float t = (i + 0.5f) / n; // 0..1
                float y = b.Top + topInset + t * (b.Height - topInset - bottomInset);
                y = Math.Clamp(y, cy - ry + 0.001f, cy + ry - 0.001f);
                float dy = y - cy;
                float term = 1f - (dy * dy) / (ry * ry);
                float dx = (float)(rx * Math.Sqrt(Math.Max(0, term)));
                float x = cx + dx; // right intersection

                // outward normal for small offset
                float nx = (x - cx) / (rx * rx);
                float ny = (y - cy) / (ry * ry);
                float nlen = MathF.Sqrt(nx * nx + ny * ny);
                if (nlen > 1e-5f) { nx /= nlen; ny /= nlen; x += nx * outwardOffset; y += ny * outwardOffset; }

                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(x, y);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(x - PortRadius, y - PortRadius, x + PortRadius, y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        /// <summary>
        /// Draw round ports using Material tokens.
        /// </summary>
        protected virtual void DrawConnectionPoints(SKCanvas canvas)
        {
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints)
            {
                var c = p.Position; canvas.DrawCircle(c.X, c.Y, PortRadius, inPaint);
            }
            foreach (var p in OutConnectionPoints)
            {
                var c = p.Position; canvas.DrawCircle(c.X, c.Y, PortRadius, outPaint);
            }
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            LayoutPorts();
            base.OnBoundsChanged(bounds);
        }
    }
}
