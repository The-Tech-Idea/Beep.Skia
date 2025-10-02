using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum ControlStatus { Planned, Implementing, Implemented, Verified }
    public enum ControlType { Preventive, Detective, Corrective, Compensating }

    public class ControlNode : SecurityControl
    {
        private string _controlName = "Control";
        private ControlType _controlType = ControlType.Preventive;
        private ControlStatus _status = ControlStatus.Planned;

        public string ControlName { get => _controlName; set { var v = value ?? string.Empty; if (_controlName != v) { _controlName = v; if (NodeProperties.TryGetValue("ControlName", out var p)) p.ParameterCurrentValue = _controlName; else NodeProperties["ControlName"] = new ParameterInfo { ParameterName = "ControlName", ParameterType = typeof(string), DefaultParameterValue = _controlName, ParameterCurrentValue = _controlName, Description = "Control name" }; Name = _controlName; InvalidateVisual(); } } }
        public ControlType ControlType { get => _controlType; set { if (_controlType != value) { _controlType = value; if (NodeProperties.TryGetValue("ControlType", out var p)) p.ParameterCurrentValue = _controlType; else NodeProperties["ControlType"] = new ParameterInfo { ParameterName = "ControlType", ParameterType = typeof(ControlType), DefaultParameterValue = _controlType, ParameterCurrentValue = _controlType, Description = "Control type", Choices = Enum.GetNames(typeof(ControlType)) }; InvalidateVisual(); } } }
        public ControlStatus Status { get => _status; set { if (_status != value) { _status = value; if (NodeProperties.TryGetValue("Status", out var p)) p.ParameterCurrentValue = _status; else NodeProperties["Status"] = new ParameterInfo { ParameterName = "Status", ParameterType = typeof(ControlStatus), DefaultParameterValue = _status, ParameterCurrentValue = _status, Description = "Implementation status", Choices = Enum.GetNames(typeof(ControlStatus)) }; InvalidateVisual(); } } }

        public ControlNode()
        {
            Width = 160; Height = 70;
            NodeProperties["ControlName"] = new ParameterInfo { ParameterName = "ControlName", ParameterType = typeof(string), DefaultParameterValue = _controlName, ParameterCurrentValue = _controlName, Description = "Control name" };
            NodeProperties["ControlType"] = new ParameterInfo { ParameterName = "ControlType", ParameterType = typeof(ControlType), DefaultParameterValue = _controlType, ParameterCurrentValue = _controlType, Description = "Control type", Choices = Enum.GetNames(typeof(ControlType)) };
            NodeProperties["Status"] = new ParameterInfo { ParameterName = "Status", ParameterType = typeof(ControlStatus), DefaultParameterValue = _status, ParameterCurrentValue = _status, Description = "Implementation status", Choices = Enum.GetNames(typeof(ControlStatus)) };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawSecurityContent(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);

            using var namePaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Edging = SKFontEdging.SubpixelAntialias, Embolden = true };
            using var metaPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8) { Edging = SKFontEdging.SubpixelAntialias };
            canvas.DrawText(ControlName, r.MidX, r.MidY, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{ControlType} · {Status}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
