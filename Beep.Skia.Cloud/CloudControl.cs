using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Cloud
{
    /// <summary>
    /// Base control for Cloud family nodes. Provides style, ports, and lazy layout.
    /// </summary>
    public abstract class CloudControl : MaterialControl
    {
        protected const float PortRadius = 4f;

        public SKColor BackgroundColor
        {
            get => _backgroundColor; set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    if (NodeProperties.TryGetValue("BackgroundColor", out var p)) p.ParameterCurrentValue = value; else NodeProperties["BackgroundColor"] = new ParameterInfo { ParameterName = "BackgroundColor", ParameterType = typeof(SKColor), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Background color" };
                    InvalidateVisual();
                }
            }
        }
        private SKColor _backgroundColor = MaterialColors.Surface;

        public SKColor BorderColor
        {
            get => _borderColor; set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    if (NodeProperties.TryGetValue("BorderColor", out var p)) p.ParameterCurrentValue = value; else NodeProperties["BorderColor"] = new ParameterInfo { ParameterName = "BorderColor", ParameterType = typeof(SKColor), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Border color" };
                    InvalidateVisual();
                }
            }
        }
        private SKColor _borderColor = MaterialColors.Outline;

        public float BorderThickness
        {
            get => _borderThickness; set
            {
                if (Math.Abs(_borderThickness - value) > float.Epsilon)
                {
                    _borderThickness = value;
                    if (NodeProperties.TryGetValue("BorderThickness", out var p)) p.ParameterCurrentValue = value; else NodeProperties["BorderThickness"] = new ParameterInfo { ParameterName = "BorderThickness", ParameterType = typeof(float), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Border thickness" };
                    InvalidateVisual();
                }
            }
        }
        private float _borderThickness = 2f;

        public int InPortCount
        {
            get => InConnectionPoints.Count; set
            {
                int v = Math.Max(0, value);
                if (InConnectionPoints.Count != v)
                {
                    EnsurePortCounts(v, OutConnectionPoints.Count);
                    if (NodeProperties.TryGetValue("InPortCount", out var p)) p.ParameterCurrentValue = v; else NodeProperties["InPortCount"] = new ParameterInfo { ParameterName = "InPortCount", ParameterType = typeof(int), DefaultParameterValue = v, ParameterCurrentValue = v, Description = "Number of inputs" };
                    InvalidateVisual();
                }
            }
        }

        public int OutPortCount
        {
            get => OutConnectionPoints.Count; set
            {
                int v = Math.Max(0, value);
                if (OutConnectionPoints.Count != v)
                {
                    EnsurePortCounts(InConnectionPoints.Count, v);
                    if (NodeProperties.TryGetValue("OutPortCount", out var p)) p.ParameterCurrentValue = v; else NodeProperties["OutPortCount"] = new ParameterInfo { ParameterName = "OutPortCount", ParameterType = typeof(int), DefaultParameterValue = v, ParameterCurrentValue = v, Description = "Number of outputs" };
                    InvalidateVisual();
                }
            }
        }

        protected CloudControl()
        {
            NodeProperties["BackgroundColor"] = new ParameterInfo { ParameterName = "BackgroundColor", ParameterType = typeof(SKColor), DefaultParameterValue = _backgroundColor, ParameterCurrentValue = _backgroundColor, Description = "Background color" };
            NodeProperties["BorderColor"] = new ParameterInfo { ParameterName = "BorderColor", ParameterType = typeof(SKColor), DefaultParameterValue = _borderColor, ParameterCurrentValue = _borderColor, Description = "Border color" };
            NodeProperties["BorderThickness"] = new ParameterInfo { ParameterName = "BorderThickness", ParameterType = typeof(float), DefaultParameterValue = _borderThickness, ParameterCurrentValue = _borderThickness, Description = "Border thickness" };
            NodeProperties["TextColor"] = new ParameterInfo { ParameterName = "TextColor", ParameterType = typeof(SKColor), DefaultParameterValue = this.TextColor, ParameterCurrentValue = this.TextColor, Description = "Text color" };
            NodeProperties["InPortCount"] = new ParameterInfo { ParameterName = "InPortCount", ParameterType = typeof(int), DefaultParameterValue = InConnectionPoints.Count, ParameterCurrentValue = InConnectionPoints.Count, Description = "Number of inputs" };
            NodeProperties["OutPortCount"] = new ParameterInfo { ParameterName = "OutPortCount", ParameterType = typeof(int), DefaultParameterValue = OutConnectionPoints.Count, ParameterCurrentValue = OutConnectionPoints.Count, Description = "Number of outputs" };
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            EnsurePortLayout(() => LayoutPorts());
            DrawCloudContent(canvas, context);
        }

        protected virtual void DrawCloudContent(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var borderPaint = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);

            if (!string.IsNullOrEmpty(Name))
            {
                using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
                using var paint = new SKPaint { Color = TextColor, IsAntialias = true };
                canvas.DrawText(Name, rect.MidX, rect.MidY + 4, SKTextAlign.Center, font, paint);
            }

            DrawPorts(canvas);
        }

        protected virtual void LayoutPorts()
        {
            LayoutPortsVerticalSegments(6f, 6f);
        }

        protected void EnsurePortCounts(int inCount, int outCount)
        {
            while (InConnectionPoints.Count < inCount)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inCount)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outCount)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outCount)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            MarkPortsDirty();
            if (NodeProperties.TryGetValue("InPortCount", out var pi)) pi.ParameterCurrentValue = InConnectionPoints.Count;
            if (NodeProperties.TryGetValue("OutPortCount", out var po)) po.ParameterCurrentValue = OutConnectionPoints.Count;
        }

        protected void LayoutPortsVerticalSegments(float topInset, float bottomInset)
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
                float cx = b.Right + 2f;
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
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, PortRadius, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, PortRadius, outPaint);
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            MarkPortsDirty();
            base.OnBoundsChanged(bounds);
        }
    }
}
