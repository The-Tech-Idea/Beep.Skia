using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Diode component for rectification and voltage regulation.
    /// </summary>
    public class ECADDiodeNode : ECADControl
    {
        private string _type = "Standard";
        private string _package = "SOD-123";
        private double _forwardVoltage = 0.7;
        private double _maxCurrent = 1.0;

        public string DiodeType { get => _type; set { var v = value ?? ""; if (_type != v) { _type = v; UpdateNodeProperty("DiodeType", _type); InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? ""; if (_package != v) { _package = v; UpdateNodeProperty("Package", _package); InvalidateVisual(); } } }
        public double ForwardVoltage { get => _forwardVoltage; set { if (Math.Abs(_forwardVoltage - value) > 0.001) { _forwardVoltage = value; UpdateNodeProperty("ForwardVoltage", _forwardVoltage); InvalidateVisual(); } } }
        public double MaxCurrent { get => _maxCurrent; set { if (Math.Abs(_maxCurrent - value) > 0.001) { _maxCurrent = value; UpdateNodeProperty("MaxCurrent", _maxCurrent); InvalidateVisual(); } } }

        public ECADDiodeNode()
        {
            Width = 80; Height = 40; Name = "Diode";
            NodeProperties["DiodeType"] = new ParameterInfo { ParameterName = "DiodeType", ParameterType = typeof(string), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Diode type", Choices = new[] { "Standard", "Zener", "Schottky", "LED", "TVS" } };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package size" };
            NodeProperties["ForwardVoltage"] = new ParameterInfo { ParameterName = "ForwardVoltage", ParameterType = typeof(double), DefaultParameterValue = _forwardVoltage, ParameterCurrentValue = _forwardVoltage, Description = "Forward voltage (V)" };
            NodeProperties["MaxCurrent"] = new ParameterInfo { ParameterName = "MaxCurrent", ParameterType = typeof(double), DefaultParameterValue = _maxCurrent, ParameterCurrentValue = _maxCurrent, Description = "Max current (A)" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw diode symbol (triangle + line)
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            using var fill = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            
            float cx = r.MidX; float cy = r.MidY; float size = 12;
            var path = new SKPath();
            path.MoveTo(cx - size, cy - size);
            path.LineTo(cx - size, cy + size);
            path.LineTo(cx + size, cy);
            path.Close();
            canvas.DrawPath(path, fill);
            canvas.DrawLine(cx + size, cy - size, cx + size, cy + size, line);

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 10, IsAntialias = true };
            canvas.DrawText(_type, r.MidX - text.MeasureText(_type) / 2, r.Bottom - 4, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
