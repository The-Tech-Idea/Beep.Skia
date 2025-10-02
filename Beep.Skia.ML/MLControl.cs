using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public abstract class MLControl : MaterialControl
    {
        protected const float PortRadius = 4f;

        private SKColor _background = MaterialColors.Surface;
        private SKColor _border = MaterialColors.Outline;
        private float _borderThickness = 2f;

        public SKColor BackgroundColor { get => _background; set { if (_background != value) { _background = value; if (NodeProperties.TryGetValue("BackgroundColor", out var p)) p.ParameterCurrentValue = value; else NodeProperties["BackgroundColor"] = new ParameterInfo { ParameterName = "BackgroundColor", ParameterType = typeof(SKColor), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Background color" }; InvalidateVisual(); } } }
        public SKColor BorderColor { get => _border; set { if (_border != value) { _border = value; if (NodeProperties.TryGetValue("BorderColor", out var p)) p.ParameterCurrentValue = value; else NodeProperties["BorderColor"] = new ParameterInfo { ParameterName = "BorderColor", ParameterType = typeof(SKColor), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Border color" }; InvalidateVisual(); } } }
        public float BorderThickness { get => _borderThickness; set { if (Math.Abs(_borderThickness - value) > float.Epsilon) { _borderThickness = value; if (NodeProperties.TryGetValue("BorderThickness", out var p)) p.ParameterCurrentValue = value; else NodeProperties["BorderThickness"] = new ParameterInfo { ParameterName = "BorderThickness", ParameterType = typeof(float), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Border thickness" }; InvalidateVisual(); } } }

        public int InPortCount { get => InConnectionPoints.Count; set { int v = Math.Max(0, value); if (InConnectionPoints.Count != v) { EnsurePortCounts(v, OutConnectionPoints.Count); if (NodeProperties.TryGetValue("InPortCount", out var p)) p.ParameterCurrentValue = v; else NodeProperties["InPortCount"] = new ParameterInfo { ParameterName = "InPortCount", ParameterType = typeof(int), DefaultParameterValue = v, ParameterCurrentValue = v, Description = "Number of inputs" }; InvalidateVisual(); } } }
        public int OutPortCount { get => OutConnectionPoints.Count; set { int v = Math.Max(0, value); if (OutConnectionPoints.Count != v) { EnsurePortCounts(InConnectionPoints.Count, v); if (NodeProperties.TryGetValue("OutPortCount", out var p)) p.ParameterCurrentValue = v; else NodeProperties["OutPortCount"] = new ParameterInfo { ParameterName = "OutPortCount", ParameterType = typeof(int), DefaultParameterValue = v, ParameterCurrentValue = v, Description = "Number of outputs" }; InvalidateVisual(); } } }

        protected MLControl()
        {
            NodeProperties["BackgroundColor"] = new ParameterInfo { ParameterName = "BackgroundColor", ParameterType = typeof(SKColor), DefaultParameterValue = _background, ParameterCurrentValue = _background, Description = "Background color" };
            NodeProperties["BorderColor"] = new ParameterInfo { ParameterName = "BorderColor", ParameterType = typeof(SKColor), DefaultParameterValue = _border, ParameterCurrentValue = _border, Description = "Border color" };
            NodeProperties["BorderThickness"] = new ParameterInfo { ParameterName = "BorderThickness", ParameterType = typeof(float), DefaultParameterValue = _borderThickness, ParameterCurrentValue = _borderThickness, Description = "Border thickness" };
            NodeProperties["TextColor"] = new ParameterInfo { ParameterName = "TextColor", ParameterType = typeof(SKColor), DefaultParameterValue = this.TextColor, ParameterCurrentValue = this.TextColor, Description = "Text color" };
            NodeProperties["InPortCount"] = new ParameterInfo { ParameterName = "InPortCount", ParameterType = typeof(int), DefaultParameterValue = InConnectionPoints.Count, ParameterCurrentValue = InConnectionPoints.Count, Description = "Number of inputs" };
            NodeProperties["OutPortCount"] = new ParameterInfo { ParameterName = "OutPortCount", ParameterType = typeof(int), DefaultParameterValue = OutConnectionPoints.Count, ParameterCurrentValue = OutConnectionPoints.Count, Description = "Number of outputs" };
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            EnsurePortLayout(() => LayoutPorts());
            DrawMLContent(canvas, context);
        }

        protected virtual void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);
        }

        protected virtual void LayoutPorts()
        {
            LayoutPortsVertical(6f, 6f);
        }

        protected void EnsurePortCounts(int inCount, int outCount)
        {
            while (InConnectionPoints.Count < inCount)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inCount) InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);
            while (OutConnectionPoints.Count < outCount)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outCount) OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);
            MarkPortsDirty();
        }

        protected void LayoutPortsVertical(float topInset, float bottomInset)
        {
            var b = Bounds;
            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = Math.Max(yTop, b.Bottom - Math.Max(0, bottomInset));
            int nIn = Math.Max(InConnectionPoints.Count, 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nIn + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Left - 2f;
                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds; cp.Index = i; cp.Component = this; cp.IsAvailable = true;
            }
            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Right + 2f;
                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds; cp.Index = i; cp.Component = this; cp.IsAvailable = true;
            }
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            MarkPortsDirty();
            base.OnBoundsChanged(bounds);
        }
    }
}
