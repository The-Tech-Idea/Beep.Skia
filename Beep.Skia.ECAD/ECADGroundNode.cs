using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Ground/Earth connection symbol.
    /// </summary>
    public class ECADGroundNode : ECADControl
    {
        private string _groundType = "Earth";

        public string GroundType { get => _groundType; set { var v = value ?? ""; if (_groundType != v) { _groundType = v; UpdateNodeProperty("GroundType", _groundType); InvalidateVisual(); } } }

        public ECADGroundNode()
        {
            Width = 50; Height = 40; Name = "Ground";
            NodeProperties["GroundType"] = new ParameterInfo { ParameterName = "GroundType", ParameterType = typeof(string), DefaultParameterValue = _groundType, ParameterCurrentValue = _groundType, Description = "Ground type", Choices = new[] { "Earth", "Chassis", "Signal", "Digital", "Analog" } };
            EnsurePortCounts(1, 0);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw ground symbol
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX; float cy = r.MidY;
            
            switch (_groundType)
            {
                case "Earth":
                    canvas.DrawLine(cx - 12, cy, cx + 12, cy, line);
                    canvas.DrawLine(cx - 8, cy + 5, cx + 8, cy + 5, line);
                    canvas.DrawLine(cx - 4, cy + 10, cx + 4, cy + 10, line);
                    break;
                case "Chassis":
                    for (int i = 0; i < 5; i++)
                    {
                        float y = cy + i * 3;
                        float w = 15 - i * 3;
                        canvas.DrawLine(cx - w, y, cx + w, y, line);
                    }
                    break;
                case "Signal":
                case "Digital":
                case "Analog":
                    var path = new SKPath();
                    path.MoveTo(cx, cy - 10);
                    path.LineTo(cx - 10, cy + 5);
                    path.LineTo(cx + 10, cy + 5);
                    path.Close();
                    canvas.DrawPath(path, line);
                    canvas.DrawLine(cx - 12, cy + 8, cx + 12, cy + 8, line);
                    break;
            }

            // Label
            using var text = new SKPaint { Color = TextColor, TextSize = 9, IsAntialias = true };
            canvas.DrawText(_groundType, r.MidX - text.MeasureText(_groundType) / 2, r.Bottom - 4, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
