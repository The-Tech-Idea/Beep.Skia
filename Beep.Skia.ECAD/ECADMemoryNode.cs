using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Memory IC (RAM, ROM, EEPROM, Flash).
    /// </summary>
    public class ECADMemoryNode : ECADControl
    {
        private string _memoryType = "EEPROM";
        private string _model = "24LC256";
        private int _capacity = 256;
        private string _interface = "I2C";
        private double _accessTime = 5.0;

        public string MemoryType { get => _memoryType; set { var v = value ?? ""; if (_memoryType != v) { _memoryType = v; UpdateNodeProperty("MemoryType", _memoryType); InvalidateVisual(); } } }
        public string Model { get => _model; set { var v = value ?? ""; if (_model != v) { _model = v; UpdateNodeProperty("Model", _model); InvalidateVisual(); } } }
        public int Capacity { get => _capacity; set { if (_capacity != value) { _capacity = value; UpdateNodeProperty("Capacity", _capacity); InvalidateVisual(); } } }
        public string Interface { get => _interface; set { var v = value ?? ""; if (_interface != v) { _interface = v; UpdateNodeProperty("Interface", _interface); InvalidateVisual(); } } }
        public double AccessTime { get => _accessTime; set { if (Math.Abs(_accessTime - value) > 0.001) { _accessTime = value; UpdateNodeProperty("AccessTime", _accessTime); InvalidateVisual(); } } }

        public ECADMemoryNode()
        {
            Width = 100; Height = 70; Name = "Memory";
            NodeProperties["MemoryType"] = new ParameterInfo { ParameterName = "MemoryType", ParameterType = typeof(string), DefaultParameterValue = _memoryType, ParameterCurrentValue = _memoryType, Description = "Memory type", Choices = new[] { "SRAM", "DRAM", "EEPROM", "Flash", "ROM" } };
            NodeProperties["Model"] = new ParameterInfo { ParameterName = "Model", ParameterType = typeof(string), DefaultParameterValue = _model, ParameterCurrentValue = _model, Description = "Model number" };
            NodeProperties["Capacity"] = new ParameterInfo { ParameterName = "Capacity", ParameterType = typeof(int), DefaultParameterValue = _capacity, ParameterCurrentValue = _capacity, Description = "Capacity (Kbit)" };
            NodeProperties["Interface"] = new ParameterInfo { ParameterName = "Interface", ParameterType = typeof(string), DefaultParameterValue = _interface, ParameterCurrentValue = _interface, Description = "Interface", Choices = new[] { "Parallel", "I2C", "SPI", "1-Wire" } };
            NodeProperties["AccessTime"] = new ParameterInfo { ParameterName = "AccessTime", ParameterType = typeof(double), DefaultParameterValue = _accessTime, ParameterCurrentValue = _accessTime, Description = "Access time (ns)" };
            EnsurePortCounts(3, 2);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw memory chip
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float inset = 10;
            var chip = new SKRect(r.Left + inset, r.Top + inset, r.Right - inset, r.Bottom - inset);
            canvas.DrawRect(chip, line);

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 10, IsAntialias = true, TextAlign = SKTextAlign.Center };
            canvas.DrawText(_memoryType, r.MidX, r.MidY - 3, text);
            
            using var small = new SKPaint { Color = TextColor, TextSize = 8, IsAntialias = true, TextAlign = SKTextAlign.Center };
            canvas.DrawText($"{_capacity}Kb", r.MidX, r.MidY + 10, small);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
