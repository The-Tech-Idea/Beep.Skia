using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Fuse or circuit breaker for overcurrent protection.
    /// </summary>
    public class ECADFuseNode : ECADControl
    {
        private string _type = "Fast";
        private double _rating = 1.0;
        private double _breakingCapacity = 100.0;
        private string _package = "Axial";

        public string FuseType { get => _type; set { var v = value ?? ""; if (_type != v) { _type = v; UpdateNodeProperty("FuseType", _type); InvalidateVisual(); } } }
        public double Rating { get => _rating; set { if (Math.Abs(_rating - value) > 0.001) { _rating = value; UpdateNodeProperty("Rating", _rating); InvalidateVisual(); } } }
        public double BreakingCapacity { get => _breakingCapacity; set { if (Math.Abs(_breakingCapacity - value) > 0.001) { _breakingCapacity = value; UpdateNodeProperty("BreakingCapacity", _breakingCapacity); InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? ""; if (_package != v) { _package = v; UpdateNodeProperty("Package", _package); InvalidateVisual(); } } }

        public ECADFuseNode()
        {
            Width = 80; Height = 40; Name = "Fuse";
            NodeProperties["FuseType"] = new ParameterInfo { ParameterName = "FuseType", ParameterType = typeof(string), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Fuse type", Choices = new[] { "Fast", "Slow", "Time-Delay", "Resettable (PTC)" } };
            NodeProperties["Rating"] = new ParameterInfo { ParameterName = "Rating", ParameterType = typeof(double), DefaultParameterValue = _rating, ParameterCurrentValue = _rating, Description = "Current rating (A)" };
            NodeProperties["BreakingCapacity"] = new ParameterInfo { ParameterName = "BreakingCapacity", ParameterType = typeof(double), DefaultParameterValue = _breakingCapacity, ParameterCurrentValue = _breakingCapacity, Description = "Breaking capacity (A)" };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package type" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw fuse symbol (rectangle with S-curve inside)
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY;
            canvas.DrawRect(cx - 20, cy - 8, 40, 16, line);
            
            // S-curve representing fuse wire
            var path = new SKPath();
            path.MoveTo(cx - 15, cy);
            path.CubicTo(cx - 10, cy - 6, cx - 5, cy + 6, cx, cy);
            path.CubicTo(cx + 5, cy - 6, cx + 10, cy + 6, cx + 15, cy);
            canvas.DrawPath(path, line);

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 10, IsAntialias = true };
            string label = $"{_rating}A";
            canvas.DrawText(label, r.MidX - text.MeasureText(label) / 2, r.Bottom - 4, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
