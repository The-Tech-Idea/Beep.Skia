using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Microcontroller/Microprocessor component.
    /// </summary>
    public class ECADMicrocontrollerNode : ECADControl
    {
        private string _model = "ATmega328P";
        private string _package = "DIP-28";
        private int _iopins = 23;
        private double _clockSpeed = 16.0;
        private int _flashMemory = 32;
        private int _sramMemory = 2;

        public string Model { get => _model; set { var v = value ?? ""; if (_model != v) { _model = v; UpdateNodeProperty("Model", _model); InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? ""; if (_package != v) { _package = v; UpdateNodeProperty("Package", _package); InvalidateVisual(); } } }
        public int IOPins { get => _iopins; set { if (_iopins != value) { _iopins = value; UpdateNodeProperty("IOPins", _iopins); InvalidateVisual(); } } }
        public double ClockSpeed { get => _clockSpeed; set { if (Math.Abs(_clockSpeed - value) > 0.001) { _clockSpeed = value; UpdateNodeProperty("ClockSpeed", _clockSpeed); InvalidateVisual(); } } }
        public int FlashMemory { get => _flashMemory; set { if (_flashMemory != value) { _flashMemory = value; UpdateNodeProperty("FlashMemory", _flashMemory); InvalidateVisual(); } } }
        public int SRAMMemory { get => _sramMemory; set { if (_sramMemory != value) { _sramMemory = value; UpdateNodeProperty("SRAMMemory", _sramMemory); InvalidateVisual(); } } }

        public ECADMicrocontrollerNode()
        {
            Width = 120; Height = 100; Name = "MCU";
            NodeProperties["Model"] = new ParameterInfo { ParameterName = "Model", ParameterType = typeof(string), DefaultParameterValue = _model, ParameterCurrentValue = _model, Description = "MCU model" };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package type" };
            NodeProperties["IOPins"] = new ParameterInfo { ParameterName = "IOPins", ParameterType = typeof(int), DefaultParameterValue = _iopins, ParameterCurrentValue = _iopins, Description = "I/O pins" };
            NodeProperties["ClockSpeed"] = new ParameterInfo { ParameterName = "ClockSpeed", ParameterType = typeof(double), DefaultParameterValue = _clockSpeed, ParameterCurrentValue = _clockSpeed, Description = "Clock speed (MHz)" };
            NodeProperties["FlashMemory"] = new ParameterInfo { ParameterName = "FlashMemory", ParameterType = typeof(int), DefaultParameterValue = _flashMemory, ParameterCurrentValue = _flashMemory, Description = "Flash memory (KB)" };
            NodeProperties["SRAMMemory"] = new ParameterInfo { ParameterName = "SRAMMemory", ParameterType = typeof(int), DefaultParameterValue = _sramMemory, ParameterCurrentValue = _sramMemory, Description = "SRAM (KB)" };
            EnsurePortCounts(4, 4);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 6, 6, body);
            canvas.DrawRoundRect(r, 6, 6, border);

            // Draw IC chip shape
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float inset = 8;
            var chipRect = new SKRect(r.Left + inset, r.Top + inset, r.Right - inset, r.Bottom - inset);
            canvas.DrawRoundRect(chipRect, 3, 3, line);
            
            // Notch at top
            canvas.DrawArc(new SKRect(chipRect.MidX - 5, chipRect.Top - 3, chipRect.MidX + 5, chipRect.Top + 3), 0, 180, false, line);

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 11, IsAntialias = true, TextAlign = SKTextAlign.Center };
            canvas.DrawText(_model, r.MidX, r.MidY - 5, text);
            
            using var small = new SKPaint { Color = TextColor, TextSize = 8, IsAntialias = true, TextAlign = SKTextAlign.Center };
            canvas.DrawText($"{_clockSpeed}MHz", r.MidX, r.MidY + 8, small);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
