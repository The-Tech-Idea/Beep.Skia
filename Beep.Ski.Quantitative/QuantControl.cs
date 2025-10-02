using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using Beep.Skia.Components;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// Base class for quantitative analysis components (e.g., time series, indicators).
    /// Provides consistent styling and standardized connection points.
    /// </summary>
    public abstract class QuantControl : MaterialControl
    {
        protected const float PortRadius = 5f;
        protected const float CornerRadius = 8f;
        protected const float Padding = 8f;

    private SKColor _fill = new SKColor(0xE0, 0xF7, 0xFA); // cyan 50
    public SKColor Fill { get => _fill; set { if (_fill == value) return; _fill = value; if (NodeProperties.TryGetValue("Fill", out var pi)) pi.ParameterCurrentValue = _fill; InvalidateVisual(); } }
    private SKColor _stroke = new SKColor(0x00, 0x96, 0x88); // teal 600
    public SKColor Stroke { get => _stroke; set { if (_stroke == value) return; _stroke = value; if (NodeProperties.TryGetValue("Stroke", out var pi)) pi.ParameterCurrentValue = _stroke; InvalidateVisual(); } }
    private float _strokeWidth = 1.5f;
    public float StrokeWidth { get => _strokeWidth; set { if (Math.Abs(_strokeWidth - value) < 0.0001f) return; _strokeWidth = value; if (NodeProperties.TryGetValue("StrokeWidth", out var pi)) pi.ParameterCurrentValue = _strokeWidth; InvalidateVisual(); } }

        protected QuantControl()
        {
            Width = Math.Max(140, Width);
            Height = Math.Max(72, Height);
            TextColor = SKColors.Black;

            // Seed NodeProperties for editor integration
            NodeProperties["Fill"] = new ParameterInfo { ParameterName = "Fill", ParameterType = typeof(SKColor), DefaultParameterValue = _fill, ParameterCurrentValue = _fill, Description = "Background fill color" };
            NodeProperties["Stroke"] = new ParameterInfo { ParameterName = "Stroke", ParameterType = typeof(SKColor), DefaultParameterValue = _stroke, ParameterCurrentValue = _stroke, Description = "Border color" };
            NodeProperties["StrokeWidth"] = new ParameterInfo { ParameterName = "StrokeWidth", ParameterType = typeof(float), DefaultParameterValue = _strokeWidth, ParameterCurrentValue = _strokeWidth, Description = "Border thickness" };
            NodeProperties["TextColor"] = new ParameterInfo { ParameterName = "TextColor", ParameterType = typeof(SKColor), DefaultParameterValue = this.TextColor, ParameterCurrentValue = this.TextColor, Description = "Text color" };
        }

        protected void EnsurePortCounts(int inCount, int outCount)
        {
            while (InConnectionPoints.Count < inCount)
                InConnectionPoints.Add(new Beep.Skia.ConnectionPoint { Type = ConnectionPointType.In, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, DataType = "series", IsAvailable = true });
            while (InConnectionPoints.Count > inCount)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outCount)
                OutConnectionPoints.Add(new Beep.Skia.ConnectionPoint { Type = ConnectionPointType.Out, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, DataType = "series", IsAvailable = true });
            while (OutConnectionPoints.Count > outCount)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            // Lazy layout: mark ports dirty and notify listeners; actual layout happens during draw
            MarkPortsDirty();
            try { OnBoundsChanged(Bounds); } catch { }
        }

        protected virtual void LayoutPorts()
        {
            float inTop = Y + Padding;
            float inBottom = Y + Height - Padding;
            PositionPortsAlongEdge(InConnectionPoints, X, inTop, inBottom, -1);
            PositionPortsAlongEdge(OutConnectionPoints, X + Width, inTop, inBottom, +1);
        }

        private void PositionPortsAlongEdge(System.Collections.Generic.List<IConnectionPoint> ports, float edgeX, float top, float bottom, int dir)
        {
            int n = Math.Max(ports.Count, 1);
            float span = Math.Max(bottom - top, 1f);
            for (int i = 0; i < ports.Count; i++)
            {
                var p = ports[i];
                float t = (i + 1) / (float)(n + 1);
                float cy = top + t * span;
                float cx = edgeX + dir * (PortRadius + 2);
                p.Center = new SKPoint(cx, cy);
                p.Position = p.Center;
                float r = PortRadius;
                p.Bounds = new SKRect(cx - r, cy - r, cx + r, cy + r);
                p.Rect = p.Bounds;
                p.Index = i;
                p.Component = this;
                p.IsAvailable = true;
            }
        }

        protected override void UpdateBounds()
        {
            base.UpdateBounds();
            // Defer port layout to DrawContent via lazy ensure
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Ensure ports are laid out lazily, then delegate to visuals
            EnsurePortLayout(() => LayoutPorts());
            DrawQuantContent(canvas, context);
        }

        /// <summary>
        /// Template method for derived quantitative nodes to draw visuals.
        /// Ports are ensured by the base DrawContent wrapper.
        /// </summary>
        protected virtual void DrawQuantContent(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = Fill, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true };
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            using var font = new SKFont { Size = 13 };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            var label = string.IsNullOrWhiteSpace(Name) ? GetType().Name : Name;
            var tb = new SKRect();
            font.MeasureText(label, out tb);
            float tx = rect.MidX - tb.Width / 2f;
            float ty = rect.MidY + tb.Height / 2f - 3f;
            canvas.DrawText(label, tx, ty, SKTextAlign.Left, font, text);

            // Draw ports
            using var inFill = new SKPaint { Color = new SKColor(0x39, 0x91, 0x7A), Style = SKPaintStyle.Fill, IsAntialias = true }; // teal
            using var outFill = new SKPaint { Color = new SKColor(0x1E, 0x88, 0xE5), Style = SKPaintStyle.Fill, IsAntialias = true }; // blue
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, inFill);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, outFill);
        }
    }
}
