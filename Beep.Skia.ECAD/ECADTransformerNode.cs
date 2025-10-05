using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Transformer for AC voltage conversion.
    /// </summary>
    public class ECADTransformerNode : ECADControl
    {
        private double _primaryVoltage = 120.0;
        private double _secondaryVoltage = 12.0;
        private double _powerRating = 10.0;
        private double _turnsRatio = 10.0;

        public double PrimaryVoltage { get => _primaryVoltage; set { if (Math.Abs(_primaryVoltage - value) > 0.001) { _primaryVoltage = value; UpdateNodeProperty("PrimaryVoltage", _primaryVoltage); InvalidateVisual(); } } }
        public double SecondaryVoltage { get => _secondaryVoltage; set { if (Math.Abs(_secondaryVoltage - value) > 0.001) { _secondaryVoltage = value; UpdateNodeProperty("SecondaryVoltage", _secondaryVoltage); InvalidateVisual(); } } }
        public double PowerRating { get => _powerRating; set { if (Math.Abs(_powerRating - value) > 0.001) { _powerRating = value; UpdateNodeProperty("PowerRating", _powerRating); InvalidateVisual(); } } }
        public double TurnsRatio { get => _turnsRatio; set { if (Math.Abs(_turnsRatio - value) > 0.001) { _turnsRatio = value; UpdateNodeProperty("TurnsRatio", _turnsRatio); InvalidateVisual(); } } }

        public ECADTransformerNode()
        {
            Width = 120; Height = 80; Name = "Transformer";
            NodeProperties["PrimaryVoltage"] = new ParameterInfo { ParameterName = "PrimaryVoltage", ParameterType = typeof(double), DefaultParameterValue = _primaryVoltage, ParameterCurrentValue = _primaryVoltage, Description = "Primary voltage (V)" };
            NodeProperties["SecondaryVoltage"] = new ParameterInfo { ParameterName = "SecondaryVoltage", ParameterType = typeof(double), DefaultParameterValue = _secondaryVoltage, ParameterCurrentValue = _secondaryVoltage, Description = "Secondary voltage (V)" };
            NodeProperties["PowerRating"] = new ParameterInfo { ParameterName = "PowerRating", ParameterType = typeof(double), DefaultParameterValue = _powerRating, ParameterCurrentValue = _powerRating, Description = "Power rating (W)" };
            NodeProperties["TurnsRatio"] = new ParameterInfo { ParameterName = "TurnsRatio", ParameterType = typeof(double), DefaultParameterValue = _turnsRatio, ParameterCurrentValue = _turnsRatio, Description = "Turns ratio" };
            EnsurePortCounts(2, 2);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw two coils with core
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY; float coilW = 20;
            
            // Primary coil
            var path1 = new SKPath();
            for (int i = 0; i < 3; i++)
            {
                float y = cy - 15 + i * 10;
                path1.AddArc(new SKRect(cx - coilW - 5, y, cx - 5, y + 10), 90, 180);
            }
            canvas.DrawPath(path1, line);
            
            // Secondary coil
            var path2 = new SKPath();
            for (int i = 0; i < 3; i++)
            {
                float y = cy - 15 + i * 10;
                path2.AddArc(new SKRect(cx + 5, y, cx + coilW + 5, y + 10), -90, 180);
            }
            canvas.DrawPath(path2, line);
            
            // Core lines
            canvas.DrawLine(cx - 2, cy - 20, cx - 2, cy + 20, line);
            canvas.DrawLine(cx + 2, cy - 20, cx + 2, cy + 20, line);

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 10, IsAntialias = true };
            string label = $"{_primaryVoltage}V:{_secondaryVoltage}V";
            canvas.DrawText(label, r.MidX - text.MeasureText(label) / 2, r.Bottom - 4, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
