using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Battery component with charge/discharge characteristics.
    /// </summary>
    public class ECADBatteryNode : ECADControl
    {
        private string _type = "Li-Ion";
        private double _voltage = 3.7;
        private double _capacity = 2000.0;
        private double _chargeRate = 1.0;
        private int _cells = 1;

        public string BatteryType { get => _type; set { var v = value ?? ""; if (_type != v) { _type = v; UpdateNodeProperty("BatteryType", _type); InvalidateVisual(); } } }
        public double Voltage { get => _voltage; set { if (Math.Abs(_voltage - value) > 0.001) { _voltage = value; UpdateNodeProperty("Voltage", _voltage); InvalidateVisual(); } } }
        public double Capacity { get => _capacity; set { if (Math.Abs(_capacity - value) > 0.001) { _capacity = value; UpdateNodeProperty("Capacity", _capacity); InvalidateVisual(); } } }
        public double ChargeRate { get => _chargeRate; set { if (Math.Abs(_chargeRate - value) > 0.001) { _chargeRate = value; UpdateNodeProperty("ChargeRate", _chargeRate); InvalidateVisual(); } } }
        public int Cells { get => _cells; set { int v = Math.Max(1, value); if (_cells != v) { _cells = v; UpdateNodeProperty("Cells", _cells); InvalidateVisual(); } } }

        public ECADBatteryNode()
        {
            Width = 90; Height = 60; Name = "Battery";
            NodeProperties["BatteryType"] = new ParameterInfo { ParameterName = "BatteryType", ParameterType = typeof(string), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Battery chemistry", Choices = new[] { "Li-Ion", "Li-Po", "NiMH", "NiCd", "Lead-Acid", "Alkaline" } };
            NodeProperties["Voltage"] = new ParameterInfo { ParameterName = "Voltage", ParameterType = typeof(double), DefaultParameterValue = _voltage, ParameterCurrentValue = _voltage, Description = "Nominal voltage (V)" };
            NodeProperties["Capacity"] = new ParameterInfo { ParameterName = "Capacity", ParameterType = typeof(double), DefaultParameterValue = _capacity, ParameterCurrentValue = _capacity, Description = "Capacity (mAh)" };
            NodeProperties["ChargeRate"] = new ParameterInfo { ParameterName = "ChargeRate", ParameterType = typeof(double), DefaultParameterValue = _chargeRate, ParameterCurrentValue = _chargeRate, Description = "Charge rate (C)" };
            NodeProperties["Cells"] = new ParameterInfo { ParameterName = "Cells", ParameterType = typeof(int), DefaultParameterValue = _cells, ParameterCurrentValue = _cells, Description = "Number of cells" };
            EnsurePortCounts(0, 2);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw battery cells
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 3, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY;
            
            for (int i = 0; i < Math.Min(_cells, 3); i++)
            {
                float x = cx - 15 + i * 15;
                canvas.DrawLine(x - 5, cy - 8, x - 5, cy + 8, line);
                canvas.DrawLine(x + 5, cy - 12, x + 5, cy + 12, new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke });
            }

            // +/- symbols
            using var text = new SKPaint { Color = BorderColor, TextSize = 12, IsAntialias = true };
            canvas.DrawText("+", cx + 20, cy + 5, text);
            canvas.DrawText("-", cx - 30, cy + 5, text);

            // Label
            using var label = new SKPaint { Color = TextColor, TextSize = 9, IsAntialias = true };
            string info = $"{_voltage * _cells}V {_capacity}mAh";
            canvas.DrawText(info, r.MidX - label.MeasureText(info) / 2, r.Bottom - 4, label);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
