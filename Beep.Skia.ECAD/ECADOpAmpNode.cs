using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Operational Amplifier (Op-Amp).
    /// </summary>
    public class ECADOpAmpNode : ECADControl
    {
        private string _model = "LM358";
        private string _package = "DIP-8";
        private double _gainBandwidth = 1.0;
        private double _supplyVoltage = 15.0;
        private double _slewRate = 0.5;

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public string Model { get => _model; set { var v = value ?? ""; if (_model != v) { _model = v; UpdateNodeProperty("Model", _model); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        public string Package { get => _package; set { var v = value ?? ""; if (_package != v) { _package = v; UpdateNodeProperty("Package", _package); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the gain bandwidth.
        /// </summary>
        public double GainBandwidth { get => _gainBandwidth; set { if (Math.Abs(_gainBandwidth - value) > 0.001) { _gainBandwidth = value; UpdateNodeProperty("GainBandwidth", _gainBandwidth); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the supply voltage.
        /// </summary>
        public double SupplyVoltage { get => _supplyVoltage; set { if (Math.Abs(_supplyVoltage - value) > 0.001) { _supplyVoltage = value; UpdateNodeProperty("SupplyVoltage", _supplyVoltage); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the slew rate.
        /// </summary>
        public double SlewRate { get => _slewRate; set { if (Math.Abs(_slewRate - value) > 0.001) { _slewRate = value; UpdateNodeProperty("SlewRate", _slewRate); InvalidateVisual(); } } }

        /// <summary>
        /// Op-amp model
        /// </summary>
        public ECADOpAmpNode()
        {
            Width = 100; Height = 80; Name = "Op-Amp";
            NodeProperties["Model"] = new ParameterInfo { ParameterName = "Model", ParameterType = typeof(string), DefaultParameterValue = _model, ParameterCurrentValue = _model, Description = "Op-amp model" };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package" };
            NodeProperties["GainBandwidth"] = new ParameterInfo { ParameterName = "GainBandwidth", ParameterType = typeof(double), DefaultParameterValue = _gainBandwidth, ParameterCurrentValue = _gainBandwidth, Description = "Gain-bandwidth (MHz)" };
            NodeProperties["SupplyVoltage"] = new ParameterInfo { ParameterName = "SupplyVoltage", ParameterType = typeof(double), DefaultParameterValue = _supplyVoltage, ParameterCurrentValue = _supplyVoltage, Description = "Supply voltage (V)" };
            NodeProperties["SlewRate"] = new ParameterInfo { ParameterName = "SlewRate", ParameterType = typeof(double), DefaultParameterValue = _slewRate, ParameterCurrentValue = _slewRate, Description = "Slew rate (V/µs)" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw triangle for op-amp
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var pathBuilder = new SKPathBuilder();
            float inset = 15;
            pathBuilder.MoveTo(r.Left + inset, r.Top + inset);
            pathBuilder.LineTo(r.Left + inset, r.Bottom - inset);
            pathBuilder.LineTo(r.Right - inset, r.MidY);
            pathBuilder.Close();
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, line);

            // +/- symbols
            using var text = new SKPaint { Color = BorderColor, IsAntialias = true };
            using var symbolFont = new SKFont(SKTypeface.Default, 12);
            canvas.DrawText("+", r.Left + inset + 5, r.Top + inset + 12, SKTextAlign.Left, symbolFont, text);
            canvas.DrawText("-", r.Left + inset + 5, r.Bottom - inset - 3, SKTextAlign.Left, symbolFont, text);

            // Label
            using var label = new SKPaint { Color = TextColor, IsAntialias = true };
            using var labelFont = new SKFont(SKTypeface.Default, 10);
            canvas.DrawText(_model, r.MidX - labelFont.MeasureText(_model) / 2, r.Bottom - 4, SKTextAlign.Left, labelFont, label);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
