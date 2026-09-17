using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Logic gate component (AND, OR, NOT, NAND, NOR, XOR, XNOR).
    /// </summary>
    public class ECADLogicGateNode : ECADControl
    {
        private string _gateType = "AND";
        private string _family = "74HC";
        private int _inputs = 2;
        private double _propagationDelay = 10.0;

        /// <summary>
        /// Gets or sets the gate type.
        /// </summary>
        public string GateType { get => _gateType; set { var v = value ?? ""; if (_gateType != v) { _gateType = v; UpdateNodeProperty("GateType", _gateType); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the logic family.
        /// </summary>
        public string LogicFamily { get => _family; set { var v = value ?? ""; if (_family != v) { _family = v; UpdateNodeProperty("LogicFamily", _family); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the num inputs.
        /// </summary>
        public int NumInputs { get => _inputs; set { int v = Math.Clamp(value, 2, 8); if (_inputs != v) { _inputs = v; EnsurePortCounts(_inputs, 1); UpdateNodeProperty("NumInputs", _inputs); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the propagation delay.
        /// </summary>
        public double PropagationDelay { get => _propagationDelay; set { if (Math.Abs(_propagationDelay - value) > 0.001) { _propagationDelay = value; UpdateNodeProperty("PropagationDelay", _propagationDelay); InvalidateVisual(); } } }

        /// <summary>
        /// Gate type
        /// </summary>
        public ECADLogicGateNode()
        {
            Width = 80; Height = 60; Name = "Logic Gate";
            NodeProperties["GateType"] = new ParameterInfo { ParameterName = "GateType", ParameterType = typeof(string), DefaultParameterValue = _gateType, ParameterCurrentValue = _gateType, Description = "Gate type", Choices = new[] { "AND", "OR", "NOT", "NAND", "NOR", "XOR", "XNOR", "Buffer" } };
            NodeProperties["LogicFamily"] = new ParameterInfo { ParameterName = "LogicFamily", ParameterType = typeof(string), DefaultParameterValue = _family, ParameterCurrentValue = _family, Description = "Logic family", Choices = new[] { "74HC", "74HCT", "74LS", "74F", "CD4000" } };
            NodeProperties["NumInputs"] = new ParameterInfo { ParameterName = "NumInputs", ParameterType = typeof(int), DefaultParameterValue = _inputs, ParameterCurrentValue = _inputs, Description = "Number of inputs" };
            NodeProperties["PropagationDelay"] = new ParameterInfo { ParameterName = "PropagationDelay", ParameterType = typeof(double), DefaultParameterValue = _propagationDelay, ParameterCurrentValue = _propagationDelay, Description = "Propagation delay (ns)" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            using var body = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 4, 4, body);
            canvas.DrawRoundRect(r, 4, 4, border);

            // Draw gate symbol
            using var line = new SKPaint { Color = BorderColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float inset = 10;
            var pathBuilder = new SKPathBuilder();
            
            switch (_gateType)
            {
                case "AND":
                case "NAND":
                    pathBuilder.MoveTo(r.Left + inset, r.Top + inset);
                    pathBuilder.LineTo(r.MidX, r.Top + inset);
                    pathBuilder.ArcTo(new SKRect(r.MidX, r.Top + inset, r.Right - inset, r.Bottom - inset), -90, 180, false);
                    pathBuilder.LineTo(r.Left + inset, r.Bottom - inset);
                    pathBuilder.Close();
                    break;
                case "OR":
                case "NOR":
                    pathBuilder.MoveTo(r.Left + inset, r.Top + inset);
                    pathBuilder.CubicTo(r.Left + 20, r.Top + inset, r.Right - 20, r.MidY - 10, r.Right - inset, r.MidY);
                    pathBuilder.CubicTo(r.Right - 20, r.MidY + 10, r.Left + 20, r.Bottom - inset, r.Left + inset, r.Bottom - inset);
                    pathBuilder.Close();
                    break;
                case "XOR":
                case "XNOR":
                    canvas.DrawArc(new SKRect(r.Left + inset - 5, r.Top + inset, r.Left + inset + 10, r.Bottom - inset), 90, 180, false, line);
                    pathBuilder.MoveTo(r.Left + inset + 5, r.Top + inset);
                    pathBuilder.CubicTo(r.Left + 25, r.Top + inset, r.Right - 20, r.MidY - 10, r.Right - inset, r.MidY);
                    pathBuilder.CubicTo(r.Right - 20, r.MidY + 10, r.Left + 25, r.Bottom - inset, r.Left + inset + 5, r.Bottom - inset);
                    pathBuilder.Close();
                    break;
                case "NOT":
                case "Buffer":
                    pathBuilder.MoveTo(r.Left + inset, r.Top + inset);
                    pathBuilder.LineTo(r.Left + inset, r.Bottom - inset);
                    pathBuilder.LineTo(r.Right - inset - (_gateType == "NOT" ? 8 : 0), r.MidY);
                    pathBuilder.Close();
                    break;
            }
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, line);

            if (_gateType.Contains("N") || _gateType == "NOT")
            {
                float bubbleX = r.Right - inset - 4;
                canvas.DrawCircle(bubbleX, r.MidY, 4, new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill });
                canvas.DrawCircle(bubbleX, r.MidY, 4, line);
            }

            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var textFont = new SKFont(SKTypeface.Default, 10);
            canvas.DrawText(_gateType, r.MidX - textFont.MeasureText(_gateType) / 2, r.Bottom - 4, SKTextAlign.Left, textFont, text);

            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
