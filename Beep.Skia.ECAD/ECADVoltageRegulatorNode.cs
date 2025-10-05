using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Voltage Regulator (linear or switching).
    /// </summary>
    public class ECADVoltageRegulatorNode : ECADControl
    {
        private string _type = "Linear";
        private string _model = "LM7805";
        private double _inputVoltage = 12.0;
        private double _outputVoltage = 5.0;
        private double _maxCurrent = 1.0;
        private double _efficiency = 60.0;

        public string RegulatorType { get => _type; set { var v = value ?? ""; if (_type != v) { _type = v; UpdateNodeProperty("RegulatorType", _type); InvalidateVisual(); } } }
        public string Model { get => _model; set { var v = value ?? ""; if (_model != v) { _model = v; UpdateNodeProperty("Model", _model); InvalidateVisual(); } } }
        public double InputVoltage { get => _inputVoltage; set { if (Math.Abs(_inputVoltage - value) > 0.001) { _inputVoltage = value; UpdateNodeProperty("InputVoltage", _inputVoltage); InvalidateVisual(); } } }
        public double OutputVoltage { get => _outputVoltage; set { if (Math.Abs(_outputVoltage - value) > 0.001) { _outputVoltage = value; UpdateNodeProperty("OutputVoltage", _outputVoltage); InvalidateVisual(); } } }
        public double MaxCurrent { get => _maxCurrent; set { if (Math.Abs(_maxCurrent - value) > 0.001) { _maxCurrent = value; UpdateNodeProperty("MaxCurrent", _maxCurrent); InvalidateVisual(); } } }
        public double Efficiency { get => _efficiency; set { if (Math.Abs(_efficiency - value) > 0.001) { _efficiency = value; UpdateNodeProperty("Efficiency", _efficiency); InvalidateVisual(); } } }

        public ECADVoltageRegulatorNode()
        {
            Width = 120; Height = 60; Name = "Voltage Regulator";
            NodeProperties["RegulatorType"] = new ParameterInfo { ParameterName = "RegulatorType", ParameterType = typeof(string), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Regulator type", Choices = new[] { "Linear", "Switching", "LDO" } };
            NodeProperties["Model"] = new ParameterInfo { ParameterName = "Model", ParameterType = typeof(string), DefaultParameterValue = _model, ParameterCurrentValue = _model, Description = "Model number" };
            NodeProperties["InputVoltage"] = new ParameterInfo { ParameterName = "InputVoltage", ParameterType = typeof(double), DefaultParameterValue = _inputVoltage, ParameterCurrentValue = _inputVoltage, Description = "Input voltage (V)" };
            NodeProperties["OutputVoltage"] = new ParameterInfo { ParameterName = "OutputVoltage", ParameterType = typeof(double), DefaultParameterValue = _outputVoltage, ParameterCurrentValue = _outputVoltage, Description = "Output voltage (V)" };
            NodeProperties["MaxCurrent"] = new ParameterInfo { ParameterName = "MaxCurrent", ParameterType = typeof(double), DefaultParameterValue = _maxCurrent, ParameterCurrentValue = _maxCurrent, Description = "Max current (A)" };
            NodeProperties["Efficiency"] = new ParameterInfo { ParameterName = "Efficiency", ParameterType = typeof(double), DefaultParameterValue = _efficiency, ParameterCurrentValue = _efficiency, Description = "Efficiency (%)" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw regulator symbol
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY;
            canvas.DrawRect(cx - 20, cy - 12, 40, 24, line);
            
            // Label inside
            using var text = new SKPaint { Color = BorderColor, TextSize = 10, IsAntialias = true, TextAlign = SKTextAlign.Center };
            canvas.DrawText("REG", cx, cy + 3, text);

            // Voltage labels
            using var label = new SKPaint { Color = TextColor, TextSize = 9, IsAntialias = true };
            canvas.DrawText($"{_outputVoltage}V", r.MidX - label.MeasureText($"{_outputVoltage}V") / 2, r.Bottom - 4, label);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
