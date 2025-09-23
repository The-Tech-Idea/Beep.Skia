using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using System;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// Base class for ERD nodes (entities, relationships, attributes).
    /// Inherits MaterialControl; provides common sizing and port layout helpers.
    /// </summary>
    public abstract class ERDControl : MaterialControl
    {
        protected const float PortRadius = 4f;
        protected const float CornerRadius = 8f;

        protected ERDControl()
        {
            Width = Math.Max(140, Width);
            Height = Math.Max(80, Height);
            ShowDisplayText = true;
            TextPosition = TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

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
            try { OnBoundsChanged(Bounds); } catch { }
        }

        protected virtual void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected void LayoutPortsVerticalSegments(float topInset, float bottomInset, float leftOffset = -2f, float rightOffset = 2f)
        {
            var b = Bounds;
            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

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

        protected void DrawPorts(SKCanvas canvas)
        {
            using var inPaint = new SKPaint { Color = new SKColor(0x42, 0xA5, 0xF5), IsAntialias = true };
            using var outPaint = new SKPaint { Color = new SKColor(0x66, 0xBB, 0x6A), IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, outPaint);
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            LayoutPorts();
            base.OnBoundsChanged(bounds);
        }
    }
}
