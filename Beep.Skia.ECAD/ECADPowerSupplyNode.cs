using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Power supply component (AC/DC, DC/DC converter).
    /// </summary>
    public class ECADPowerSupplyNode : ECADControl
    {
        private string _type = "AC-DC";
        private double _inputVoltage = 120.0;
        private double _outputVoltage = 12.0;
        private double _outputCurrent = 5.0;
        private double _efficiency = 85.0;

        public string SupplyType { get => _type; set { var v = value ?? ""; if (_type != v) { _type = v; UpdateNodeProperty("SupplyType", _type); InvalidateVisual(); } } }
        public double InputVoltage { get => _inputVoltage; set { if (Math.Abs(_inputVoltage - value) > 0.001) { _inputVoltage = value; UpdateNodeProperty("InputVoltage", _inputVoltage); InvalidateVisual(); } } }
        public double OutputVoltage { get => _outputVoltage; set { if (Math.Abs(_outputVoltage - value) > 0.001) { _outputVoltage = value; UpdateNodeProperty("OutputVoltage", _outputVoltage); InvalidateVisual(); } } }
        public double OutputCurrent { get => _outputCurrent; set { if (Math.Abs(_outputCurrent - value) > 0.001) { _outputCurrent = value; UpdateNodeProperty("OutputCurrent", _outputCurrent); InvalidateVisual(); } } }
        public double Efficiency { get => _efficiency; set { if (Math.Abs(_efficiency - value) > 0.001) { _efficiency = value; UpdateNodeProperty("Efficiency", _efficiency); InvalidateVisual(); } } }

        public ECADPowerSupplyNode()
        {
            Width = 130; Height = 70; Name = "Power Supply";
            NodeProperties["SupplyType"] = new ParameterInfo { ParameterName = "SupplyType", ParameterType = typeof(string), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Supply type", Choices = new[] { "AC-DC", "DC-DC Buck", "DC-DC Boost", "DC-DC Buck-Boost", "Inverter" } };
            NodeProperties["InputVoltage"] = new ParameterInfo { ParameterName = "InputVoltage", ParameterType = typeof(double), DefaultParameterValue = _inputVoltage, ParameterCurrentValue = _inputVoltage, Description = "Input voltage (V)" };
            NodeProperties["OutputVoltage"] = new ParameterInfo { ParameterName = "OutputVoltage", ParameterType = typeof(double), DefaultParameterValue = _outputVoltage, ParameterCurrentValue = _outputVoltage, Description = "Output voltage (V)" };
            NodeProperties["OutputCurrent"] = new ParameterInfo { ParameterName = "OutputCurrent", ParameterType = typeof(double), DefaultParameterValue = _outputCurrent, ParameterCurrentValue = _outputCurrent, Description = "Output current (A)" };
            NodeProperties["Efficiency"] = new ParameterInfo { ParameterName = "Efficiency", ParameterType = typeof(double), DefaultParameterValue = _efficiency, ParameterCurrentValue = _efficiency, Description = "Efficiency (%)" };
            EnsurePortCounts(2, 2);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw power supply symbol
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY;
            canvas.DrawRect(cx - 25, cy - 15, 50, 30, line);
            
            // Arrow showing conversion
            var arrow = new SKPath();
            arrow.MoveTo(cx - 15, cy);
            arrow.LineTo(cx + 10, cy);
            arrow.LineTo(cx + 5, cy - 5);
            arrow.MoveTo(cx + 10, cy);
            arrow.LineTo(cx + 5, cy + 5);
            canvas.DrawPath(arrow, line);

            // Labels
            using var text = new SKPaint { Color = TextColor, TextSize = 9, IsAntialias = true };
            canvas.DrawText($"{_inputVoltage}V", r.Left + 5, r.Top + 12, text);
            canvas.DrawText($"{_outputVoltage}V", r.Right - 30, r.Top + 12, text);
            canvas.DrawText($"{_outputCurrent}A", r.MidX - text.MeasureText($"{_outputCurrent}A") / 2, r.Bottom - 4, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
