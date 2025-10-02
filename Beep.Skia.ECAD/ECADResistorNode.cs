using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    public enum Orientation { Horizontal, Vertical }

    public class ECADResistorNode : ECADControl
    {
        private string _value = "1kΩ";
        private string _package = "0603";
        private Orientation _orientation = Orientation.Horizontal;

        public string ComponentValue { get => _value; set { var v = value ?? string.Empty; if (_value != v) { _value = v; if (NodeProperties.TryGetValue("ComponentValue", out var p)) p.ParameterCurrentValue = _value; else NodeProperties["ComponentValue"] = new ParameterInfo { ParameterName = "ComponentValue", ParameterType = typeof(string), DefaultParameterValue = _value, ParameterCurrentValue = _value, Description = "Resistor value" }; InvalidateVisual(); } } }
        public string Package { get => _package; set { var v = value ?? string.Empty; if (_package != v) { _package = v; if (NodeProperties.TryGetValue("Package", out var p)) p.ParameterCurrentValue = _package; else NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package" }; InvalidateVisual(); } } }
        public Orientation Orientation { get => _orientation; set { if (_orientation != value) { _orientation = value; if (NodeProperties.TryGetValue("Orientation", out var p)) p.ParameterCurrentValue = _orientation; else NodeProperties["Orientation"] = new ParameterInfo { ParameterName = "Orientation", ParameterType = typeof(Orientation), DefaultParameterValue = _orientation, ParameterCurrentValue = _orientation, Description = "Orientation", Choices = Enum.GetNames(typeof(Orientation)) }; InvalidateVisual(); } } }

        public ECADResistorNode()
        {
            Width = 100; Height = 40; Name = "Resistor";
            NodeProperties["ComponentValue"] = new ParameterInfo { ParameterName = "ComponentValue", ParameterType = typeof(string), DefaultParameterValue = _value, ParameterCurrentValue = _value, Description = "Resistor value" };
            NodeProperties["Package"] = new ParameterInfo { ParameterName = "Package", ParameterType = typeof(string), DefaultParameterValue = _package, ParameterCurrentValue = _package, Description = "Package" };
            NodeProperties["Orientation"] = new ParameterInfo { ParameterName = "Orientation", ParameterType = typeof(Orientation), DefaultParameterValue = _orientation, ParameterCurrentValue = _orientation, Description = "Orientation", Choices = Enum.GetNames(typeof(Orientation)) };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            // Body
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Zig-zag for resistor
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var path = new SKPath();
            if (Orientation == Orientation.Horizontal)
            {
                float y = r.MidY; float x = r.Left + 8; float step = (r.Width - 16) / 6f;
                path.MoveTo(x, y);
                for (int i = 0; i < 6; i++)
                {
                    float x1 = x + i * step;
                    float x2 = x1 + step;
                    float y1 = (i % 2 == 0) ? y - 8 : y + 8;
                    path.LineTo(x1 + step / 2, y1);
                    path.LineTo(x2, y);
                }
            }
            else
            {
                float x = r.MidX; float y = r.Top + 8; float step = (r.Height - 16) / 6f;
                path.MoveTo(x, y);
                for (int i = 0; i < 6; i++)
                {
                    float y1 = y + i * step;
                    float y2 = y1 + step;
                    float x1 = (i % 2 == 0) ? x - 8 : x + 8;
                    path.LineTo(x1, y1 + step / 2);
                    path.LineTo(x, y2);
                }
            }
            canvas.DrawPath(path, line);

            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            canvas.DrawText(ComponentValue + " " + Package, r.MidX, r.Bottom + 12, SKTextAlign.Center, font, textPaint);

            // Ports
            DrawPorts(canvas);
        }
    }
}
