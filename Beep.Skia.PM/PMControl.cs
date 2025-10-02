using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.PM
{
    /// <summary>
    /// Base class for Project Management (PM) nodes with consistent sizing and port helpers.
    /// </summary>
    public abstract class PMControl : MaterialControl
    {
        protected const float PortRadius = 4f;
        protected const float CornerRadius = 8f;

        protected PMControl()
        {
            Width = Math.Max(140, Width);
            Height = Math.Max(60, Height);
            ShowDisplayText = true;
            TextPosition = TextPosition.Below;
            EnsurePortCounts(1, 1);

            // Seed common PM metadata for editors
            NodeProperties["InPortCount"] = new ParameterInfo
            {
                ParameterName = "InPortCount",
                ParameterType = typeof(int),
                DefaultParameterValue = InPortCount,
                ParameterCurrentValue = InPortCount,
                Description = "Number of input ports (left)."
            };
            NodeProperties["OutPortCount"] = new ParameterInfo
            {
                ParameterName = "OutPortCount",
                ParameterType = typeof(int),
                DefaultParameterValue = OutPortCount,
                ParameterCurrentValue = OutPortCount,
                Description = "Number of output ports (right)."
            };
        }

        public int InPortCount
        {
            get => InConnectionPoints?.Count ?? 0;
            set
            {
                int v = Math.Max(0, value);
                EnsurePortCounts(v, OutPortCount);
                if (NodeProperties.TryGetValue("InPortCount", out var pi))
                    pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public int OutPortCount
        {
            get => OutConnectionPoints?.Count ?? 0;
            set
            {
                int v = Math.Max(0, value);
                EnsurePortCounts(InPortCount, v);
                if (NodeProperties.TryGetValue("OutPortCount", out var pi))
                    pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
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

            // Keep NodeProperties aligned with programmatic changes
            if (NodeProperties.TryGetValue("InPortCount", out var piIn))
                piIn.ParameterCurrentValue = InConnectionPoints.Count;
            if (NodeProperties.TryGetValue("OutPortCount", out var piOut))
                piOut.ParameterCurrentValue = OutConnectionPoints.Count;

            MarkPortsDirty();
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
                float cx = b.Left - Math.Abs(leftOffset);
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
                float cx = b.Right + Math.Abs(rightOffset);
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
            // Use Material Design tokens for consistent theming across families
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, outPaint);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            EnsurePortLayout(() => LayoutPorts());
            DrawPMContent(canvas, context);
        }

        protected virtual void DrawPMContent(SKCanvas canvas, DrawingContext context) { }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            MarkPortsDirty();
            base.OnBoundsChanged(bounds);
        }
    }
}
