using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;
using SkiaSharp;
using System;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// Base class for DFD visual nodes. Pure component (no editor), registers ports and draws label/ports.
    /// </summary>
    public abstract class DFDControl : MaterialControl
    {
        protected const float PortRadius = 4f;
        protected const float CornerRadius = 14f;

        protected DFDControl()
        {
            Width = Math.Max(140, Width);
            Height = Math.Max(90, Height);
            ShowDisplayText = true;
            TextPosition = TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected void EnsurePortCounts(int inputs, int outputs)
        {
            while (InConnectionPoints.Count < inputs)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "data", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inputs)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outputs)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "data", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outputs)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            LayoutPorts();
            try { OnBoundsChanged(Bounds); } catch { }
        }

        protected virtual void LayoutPorts()
        {
            // Default rectangle layout with minimal top/bottom padding
            LayoutPortsVerticalSegments(topInset: 4f, bottomInset: 4f);
        }

        /// <summary>
        /// Layout input (left) and output (right) ports along the usable straight-edge segments
        /// of the component, honoring top/bottom insets to avoid rounded corners or decorations.
        /// Ports are positioned slightly outside the shape so connection lines start outside the fill.
        /// </summary>
        /// <param name="topInset">Padding from top edge to start placing ports.</param>
        /// <param name="bottomInset">Padding from bottom edge to stop placing ports.</param>
        /// <param name="leftOffset">Horizontal offset from the left edge (negative means outside).</param>
        /// <param name="rightOffset">Horizontal offset from the right edge (positive means outside).</param>
        protected void LayoutPortsVerticalSegments(float topInset, float bottomInset, float leftOffset = -2f, float rightOffset = 2f)
        {
            var b = Bounds;
            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            // Inputs on left edge
            int nIn = Math.Max(InConnectionPoints.Count, 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nIn + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Left + leftOffset;
                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            // Outputs on right edge
            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Right + rightOffset;
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
        /// Layout input/output ports on the perimeter of an ellipse fitting the given bounds.
        /// Ports are distributed vertically between top/bottom insets and placed at the exact left/right
        /// intersection with the ellipse. Optionally offsets points slightly outward along the surface normal.
        /// </summary>
        /// <param name="topInset">Padding from top of the ellipse's bounding box.</param>
        /// <param name="bottomInset">Padding from bottom of the ellipse's bounding box.</param>
        /// <param name="outwardOffset">Distance to push the port outward from the ellipse along the normal.</param>
        protected void LayoutPortsOnEllipse(float topInset, float bottomInset, float outwardOffset = 2f)
        {
            var b = Bounds;
            float cx = b.MidX;
            float cy = b.MidY;
            float rx = Math.Max(1f, b.Width / 2f);
            float ry = Math.Max(1f, b.Height / 2f);

            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            int nIn = Math.Max(InConnectionPoints.Count, 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nIn + 1);
                float y = yTop + t * (yBottom - yTop);
                // Clamp y inside ellipse vertical extent
                y = Math.Clamp(y, cy - ry + 0.001f, cy + ry - 0.001f);
                float dy = y - cy;
                // Ellipse equation: (x-cx)^2/rx^2 + (y-cy)^2/ry^2 = 1 => solve for x at given y
                float term = 1f - (dy * dy) / (ry * ry);
                float dx = (float)(rx * Math.Sqrt(Math.Max(0, term)));
                float x = cx - dx; // left intersection

                // Outward normal for ellipse F(x,y)= (x-cx)^2/rx^2 + (y-cy)^2/ry^2 -1
                // grad F = (2(x-cx)/rx^2, 2(y-cy)/ry^2)
                float nx = (x - cx) / (rx * rx);
                float ny = (y - cy) / (ry * ry);
                float nlen = MathF.Sqrt(nx * nx + ny * ny);
                if (nlen > 1e-5f)
                {
                    nx /= nlen; ny /= nlen;
                    x += nx * outwardOffset; y += ny * outwardOffset;
                }

                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(x, y);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(x - PortRadius, y - PortRadius, x + PortRadius, y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float y = yTop + t * (yBottom - yTop);
                y = Math.Clamp(y, cy - ry + 0.001f, cy + ry - 0.001f);
                float dy = y - cy;
                float term = 1f - (dy * dy) / (ry * ry);
                float dx = (float)(rx * Math.Sqrt(Math.Max(0, term)));
                float x = cx + dx; // right intersection

                float nx = (x - cx) / (rx * rx);
                float ny = (y - cy) / (ry * ry);
                float nlen = MathF.Sqrt(nx * nx + ny * ny);
                if (nlen > 1e-5f)
                {
                    nx /= nlen; ny /= nlen;
                    x += nx * outwardOffset; y += ny * outwardOffset;
                }

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

        protected override void OnBoundsChanged(SKRect bounds)
        {
            // Keep ports aligned to the current shape geometry whenever the bounds change
            LayoutPorts();
            base.OnBoundsChanged(bounds);
        }

        protected void DrawPorts(SKCanvas canvas)
        {
            using var inPaint = new SKPaint { Color = new SKColor(0x42, 0xA5, 0xF5), IsAntialias = true };
            using var outPaint = new SKPaint { Color = new SKColor(0x66, 0xBB, 0x6A), IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, outPaint);
        }
    }
}
