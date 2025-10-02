using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    public class ECADCapacitorNode : ECADControl
    {
        private string _value = "10uF";
        private string _package = "0603";
        private Orientation _orientation = Orientation.Horizontal;

        public string ComponentValue { get => _value; set { var v = value ?? string.Empty; if (_value != v) { _value = v; if (NodeProperties.TryGetValue("ComponentValue", out var p)) p.ParameterCurrentValue = _value; else NodeProperties["ComponentValue"] = new ParameterInfo { ParameterName = "ComponentValue", ParameterType = typeof(string), DefaultParameterValue = _value, ParameterCurrentValue = _value, Description = "Capacitor value" }; InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? string.Empty; if (_package != v) { _package = v; if (NodeProperties.TryGetValue("Package", out var p)) p.ParameterCurrentValue = _package; else NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package" }; InvalidateVisual(); } } }
        public Orientation Orientation { get => _orientation; set { if (_orientation != value) { _orientation = value; if (NodeProperties.TryGetValue("Orientation", out var p)) p.ParameterCurrentValue = _orientation; else NodeProperties["Orientation"] = new ParameterInfo { ParameterName = "Orientation", ParameterType = typeof(Orientation), DefaultParameterValue = _orientation, ParameterCurrentValue = _orientation, Description = "Orientation", Choices = Enum.GetNames(typeof(Orientation)) }; InvalidateVisual(); } } }

        public ECADCapacitorNode()
        {
            Width = 100; Height = 40; Name = "Capacitor";
            NodeProperties["ComponentValue"] = new ParameterInfo { ParameterName = "ComponentValue", ParameterType = typeof(string), DefaultParameterValue = _value, ParameterCurrentValue = _value, Description = "Capacitor value" };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package" };
            NodeProperties["Orientation"] = new ParameterInfo { ParameterName = "Orientation", ParameterType = typeof(Orientation), DefaultParameterValue = _orientation, ParameterCurrentValue = _orientation, Description = "Orientation", Choices = Enum.GetNames(typeof(Orientation)) };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            if (Orientation == Orientation.Horizontal)
            {
                float y = r.MidY;
                canvas.DrawLine(r.Left + 10, y - 6, r.MidX - 6, y - 6, line);
                canvas.DrawLine(r.Left + 10, y + 6, r.MidX - 6, y + 6, line);
                canvas.DrawLine(r.MidX + 6, y - 6, r.Right - 10, y - 6, line);
                canvas.DrawLine(r.MidX + 6, y + 6, r.Right - 10, y + 6, line);
            }
            else
            {
                float x = r.MidX;
                canvas.DrawLine(x - 6, r.Top + 10, x - 6, r.MidY - 6, line);
                canvas.DrawLine(x + 6, r.Top + 10, x + 6, r.MidY - 6, line);
                canvas.DrawLine(x - 6, r.MidY + 6, x - 6, r.Bottom - 10, line);
                canvas.DrawLine(x + 6, r.MidY + 6, x + 6, r.Bottom - 10, line);
            }

            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            canvas.DrawText(ComponentValue + " " + Package, r.MidX, r.Bottom + 12, SKTextAlign.Center, font, textPaint);

            DrawPorts(canvas);
        }
    }
}
