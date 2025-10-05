using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Inductor component for storing energy in magnetic fields.
    /// </summary>
    public class ECADInductorNode : ECADControl
    {
        private string _value = "10µH";
        private string _package = "0805";
        private double _tolerance = 10.0;
        private double _current = 1.0;

        public string ComponentValue { get => _value; set { var v = value ?? ""; if (_value != v) { _value = v; UpdateNodeProperty("ComponentValue", _value); InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? ""; if (_package != v) { _package = v; UpdateNodeProperty("Package", _package); InvalidateVisual(); } } }
        public double Tolerance { get => _tolerance; set { if (Math.Abs(_tolerance - value) > 0.001) { _tolerance = value; UpdateNodeProperty("Tolerance", _tolerance); InvalidateVisual(); } } }
        public double RatedCurrent { get => _current; set { if (Math.Abs(_current - value) > 0.001) { _current = value; UpdateNodeProperty("RatedCurrent", _current); InvalidateVisual(); } } }

        public ECADInductorNode()
        {
            Width = 100; Height = 40; Name = "Inductor";
            NodeProperties["ComponentValue"] = new ParameterInfo { ParameterName = "ComponentValue", ParameterType = typeof(string), DefaultParameterValue = _value, ParameterCurrentValue = _value, Description = "Inductance value" };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package size" };
            NodeProperties["Tolerance"] = new ParameterInfo { ParameterName = "Tolerance", ParameterType = typeof(double), DefaultParameterValue = _tolerance, ParameterCurrentValue = _tolerance, Description = "Tolerance (%)" };
            NodeProperties["RatedCurrent"] = new ParameterInfo { ParameterName = "RatedCurrent", ParameterType = typeof(double), DefaultParameterValue = _current, ParameterCurrentValue = _current, Description = "Rated current (A)" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw inductor coils
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float y = r.MidY; float startX = r.Left + 10; float coilWidth = (r.Width - 20) / 4f;
            var path = new SKPath();
            path.MoveTo(startX, y);
            for (int i = 0; i < 4; i++)
            {
                float x = startX + i * coilWidth;
                path.ArcTo(new SKRect(x, y - 10, x + coilWidth, y + 10), 180, 180, false);
            }
            canvas.DrawPath(path, line);

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 10, IsAntialias = true };
            canvas.DrawText(_value, r.MidX - text.MeasureText(_value) / 2, r.Bottom - 4, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
