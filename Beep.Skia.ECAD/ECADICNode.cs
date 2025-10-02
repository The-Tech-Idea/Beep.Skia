using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ECAD
{
    public class ECADICNode : ECADControl
    {
        private string _reference = "U1";
        private string _footprint = "QFN-32";
        private int _pinCount = 16;

        public string Reference { get => _reference; set { var v = value ?? string.Empty; if (_reference != v) { _reference = v; if (NodeProperties.TryGetValue("Reference", out var p)) p.ParameterCurrentValue = _reference; else NodeProperties["Reference"] = new ParameterInfo { ParameterName = "Reference", ParameterType = typeof(string), DefaultParameterValue = _reference, ParameterCurrentValue = _reference, Description = "Component reference" }; Name = _reference; InvalidateVisual(); } } }
        public string Footprint { get => _footprint; set { var v = value ?? string.Empty; if (_footprint != v) { _footprint = v; if (NodeProperties.TryGetValue("Footprint", out var p)) p.ParameterCurrentValue = _footprint; else NodeProperties["Footprint"] = new ParameterInfo { ParameterName = "Footprint", ParameterType = typeof(string), DefaultParameterValue = _footprint, ParameterCurrentValue = _footprint, Description = "Footprint" }; InvalidateVisual(); MarkPortsDirty(); } } }
        public int PinCount { get => _pinCount; set { var v = Math.Max(1, value); if (_pinCount != v) { _pinCount = v; if (NodeProperties.TryGetValue("PinCount", out var p)) p.ParameterCurrentValue = _pinCount; else NodeProperties["PinCount"] = new ParameterInfo { ParameterName = "PinCount", ParameterType = typeof(int), DefaultParameterValue = _pinCount, ParameterCurrentValue = _pinCount, Description = "Pin count" }; InvalidateVisual(); MarkPortsDirty(); } } }

        public ECADICNode()
        {
            Width = 140; Height = 100;
            NodeProperties["Reference"] = new ParameterInfo { ParameterName = "Reference", ParameterType = typeof(string), DefaultParameterValue = _reference, ParameterCurrentValue = _reference, Description = "Component reference" };
            NodeProperties["Footprint"] = new ParameterInfo { ParameterName = "Footprint", ParameterType = typeof(string), DefaultParameterValue = _footprint, ParameterCurrentValue = _footprint, Description = "Footprint" };
            NodeProperties["PinCount"] = new ParameterInfo { ParameterName = "PinCount", ParameterType = typeof(int), DefaultParameterValue = _pinCount, ParameterCurrentValue = _pinCount, Description = "Pin count" };
            EnsurePortCounts(4, 4);
        }

        protected override void DrawECADContent(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, IsAntialias = true, Style = SKPaintStyle.Stroke };
            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, border);

            using var namePaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var metaPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(Reference, r.MidX, r.MidY - 6, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText(Footprint, r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var pinPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 3, pinPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 3, pinPaint);
        }
    }
}
