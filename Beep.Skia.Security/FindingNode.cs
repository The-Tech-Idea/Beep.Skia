using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum FindingType { Misconfiguration, Vulnerability, SuspiciousActivity, PolicyViolation }

    public class FindingNode : SecurityControl
    {
        private string _title = "Finding";
        private FindingType _type = FindingType.Misconfiguration;
        private Confidence _confidence = Confidence.Medium;

        public string Title { get => _title; set { var v = value ?? string.Empty; if (_title != v) { _title = v; if (NodeProperties.TryGetValue("Title", out var p)) p.ParameterCurrentValue = _title; else NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Finding title" }; Name = _title; InvalidateVisual(); } } }
        public FindingType Type { get => _type; set { if (_type != value) { _type = value; if (NodeProperties.TryGetValue("Type", out var p)) p.ParameterCurrentValue = _type; else NodeProperties["Type"] = new ParameterInfo { ParameterName = "Type", ParameterType = typeof(FindingType), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Finding type", Choices = Enum.GetNames(typeof(FindingType)) }; InvalidateVisual(); } } }
        public Confidence Confidence { get => _confidence; set { if (_confidence != value) { _confidence = value; if (NodeProperties.TryGetValue("Confidence", out var p)) p.ParameterCurrentValue = _confidence; else NodeProperties["Confidence"] = new ParameterInfo { ParameterName = "Confidence", ParameterType = typeof(Confidence), DefaultParameterValue = _confidence, ParameterCurrentValue = _confidence, Description = "Detection confidence", Choices = Enum.GetNames(typeof(Confidence)) }; InvalidateVisual(); } } }

        public FindingNode()
        {
            Width = 160; Height = 80;
            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Finding title" };
            NodeProperties["Type"] = new ParameterInfo { ParameterName = "Type", ParameterType = typeof(FindingType), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Finding type", Choices = Enum.GetNames(typeof(FindingType)) };
            NodeProperties["Confidence"] = new ParameterInfo { ParameterName = "Confidence", ParameterType = typeof(Confidence), DefaultParameterValue = _confidence, ParameterCurrentValue = _confidence, Description = "Detection confidence", Choices = Enum.GetNames(typeof(Confidence)) };
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
            canvas.DrawText(Title, r.MidX, r.MidY, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Type} · {Confidence}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
