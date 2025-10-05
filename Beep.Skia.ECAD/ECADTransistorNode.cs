using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Transistor component (BJT or MOSFET).
    /// </summary>
    public class ECADTransistorNode : ECADControl
    {
        private string _type = "NPN";
        private string _package = "SOT-23";
        private double _maxVoltage = 40.0;
        private double _maxCurrent = 0.5;
        private double _powerDissipation = 0.35;

        public string TransistorType { get => _type; set { var v = value ?? ""; if (_type != v) { _type = v; UpdateNodeProperty("TransistorType", _type); InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? ""; if (_package != v) { _package = v; UpdateNodeProperty("Package", _package); InvalidateVisual(); } } }
        public double MaxVoltage { get => _maxVoltage; set { if (Math.Abs(_maxVoltage - value) > 0.001) { _maxVoltage = value; UpdateNodeProperty("MaxVoltage", _maxVoltage); InvalidateVisual(); } } }
        public double MaxCurrent { get => _maxCurrent; set { if (Math.Abs(_maxCurrent - value) > 0.001) { _maxCurrent = value; UpdateNodeProperty("MaxCurrent", _maxCurrent); InvalidateVisual(); } } }
        public double PowerDissipation { get => _powerDissipation; set { if (Math.Abs(_powerDissipation - value) > 0.001) { _powerDissipation = value; UpdateNodeProperty("PowerDissipation", _powerDissipation); InvalidateVisual(); } } }

        public ECADTransistorNode()
        {
            Width = 80; Height = 60; Name = "Transistor";
            NodeProperties["TransistorType"] = new ParameterInfo { ParameterName = "TransistorType", ParameterType = typeof(string), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Transistor type", Choices = new[] { "NPN", "PNP", "N-MOSFET", "P-MOSFET", "JFET" } };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package" };
            NodeProperties["MaxVoltage"] = new ParameterInfo { ParameterName = "MaxVoltage", ParameterType = typeof(double), DefaultParameterValue = _maxVoltage, ParameterCurrentValue = _maxVoltage, Description = "Max voltage (V)" };
            NodeProperties["MaxCurrent"] = new ParameterInfo { ParameterName = "MaxCurrent", ParameterType = typeof(double), DefaultParameterValue = _maxCurrent, ParameterCurrentValue = _maxCurrent, Description = "Max current (A)" };
            NodeProperties["PowerDissipation"] = new ParameterInfo { ParameterName = "PowerDissipation", ParameterType = typeof(double), DefaultParameterValue = _powerDissipation, ParameterCurrentValue = _powerDissipation, Description = "Power dissipation (W)" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw transistor symbol
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY;
            
            canvas.DrawLine(cx, cy - 12, cx, cy + 12, line);
            canvas.DrawLine(cx - 15, cy, cx, cy, line);
            canvas.DrawLine(cx, cy - 12, cx + 15, cy - 18, line);
            canvas.DrawLine(cx, cy + 12, cx + 15, cy + 18, line);
            
            if (_type.Contains("NPN") || _type.Contains("PNP"))
            {
                var arrow = new SKPath();
                if (_type == "NPN")
                {
                    arrow.MoveTo(cx + 12, cy + 15);
                    arrow.LineTo(cx + 10, cy + 10);
                    arrow.LineTo(cx + 15, cy + 13);
                    arrow.Close();
                }
                else
                {
                    arrow.MoveTo(cx + 3, cy + 8);
                    arrow.LineTo(cx, cy + 12);
                    arrow.LineTo(cx + 5, cy + 12);
                    arrow.Close();
                }
                canvas.DrawPath(arrow, new SKPaint { Color = BorderColor, Style = SKPaintStyle.Fill });
            }

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
