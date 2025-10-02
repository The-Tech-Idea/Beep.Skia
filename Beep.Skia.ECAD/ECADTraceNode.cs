using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    public enum Layer { Top, Bottom, Inner1, Inner2 }

    public class ECADTraceNode : ECADControl
    {
        private float _width = 1.0f; // px
        private Layer _layer = Layer.Top;
        private SKPoint _start;
        private SKPoint _end;

        public float WidthPx { get => _width; set { var v = Math.Max(0.1f, Math.Min(10f, value)); if (Math.Abs(_width - v) > float.Epsilon) { _width = v; if (NodeProperties.TryGetValue("WidthPx", out var p)) p.ParameterCurrentValue = _width; else NodeProperties["WidthPx"] = new ParameterInfo { ParameterName = "WidthPx", ParameterType = typeof(float), DefaultParameterValue = _width, ParameterCurrentValue = _width, Description = "Trace width (px)" }; InvalidateVisual(); } } }
        public Layer Layer { get => _layer; set { if (_layer != value) { _layer = value; if (NodeProperties.TryGetValue("Layer", out var p)) p.ParameterCurrentValue = _layer; else NodeProperties["Layer"] = new ParameterInfo { ParameterName = "Layer", ParameterType = typeof(Layer), DefaultParameterValue = _layer, ParameterCurrentValue = _layer, Description = "Layer", Choices = Enum.GetNames(typeof(Layer)) }; InvalidateVisual(); } } }
        public SKPoint Start { get => _start; set { if (_start != value) { _start = value; InvalidateVisual(); } } }
        public SKPoint End { get => _end; set { if (_end != value) { _end = value; InvalidateVisual(); } } }

        public ECADTraceNode()
        {
            Width = 100; Height = 30;
            EnsurePortCounts(0, 0);
            NodeProperties["WidthPx"] = new ParameterInfo { ParameterName = "WidthPx", ParameterType = typeof(float), DefaultParameterValue = _width, ParameterCurrentValue = _width, Description = "Trace width (px)" };
            NodeProperties["Layer"] = new ParameterInfo { ParameterName = "Layer", ParameterType = typeof(Layer), DefaultParameterValue = _layer, ParameterCurrentValue = _layer, Description = "Layer", Choices = Enum.GetNames(typeof(Layer)) };
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var paint = new SKPaint { Color = BorderColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = WidthPx };
            var y = Y + Height / 2f;
            // If Start/End not set, draw a centered segment; otherwise draw between points
            if (Start == default && End == default)
            {
                canvas.DrawLine(X, y, X + Width, y, paint);
            }
            else
            {
                canvas.DrawLine(Start, End, paint);
            }
        }
    }
}
