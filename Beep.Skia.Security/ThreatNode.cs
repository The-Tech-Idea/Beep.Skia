using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum Severity { Low, Medium, High, Critical }
    public enum Likelihood { Rare, Unlikely, Possible, Likely, AlmostCertain }

    public class ThreatNode : SecurityControl
    {
        private string _threatName = "Threat";
        private Severity _severity = Severity.Medium;
        private Likelihood _likelihood = Likelihood.Possible;

        public string ThreatName { get => _threatName; set { var v = value ?? string.Empty; if (_threatName != v) { _threatName = v; if (NodeProperties.TryGetValue("ThreatName", out var p)) p.ParameterCurrentValue = _threatName; else NodeProperties["ThreatName"] = new ParameterInfo { ParameterName = "ThreatName", ParameterType = typeof(string), DefaultParameterValue = _threatName, ParameterCurrentValue = _threatName, Description = "Threat name" }; Name = _threatName; InvalidateVisual(); } } }
        public Severity Severity { get => _severity; set { if (_severity != value) { _severity = value; if (NodeProperties.TryGetValue("Severity", out var p)) p.ParameterCurrentValue = _severity; else NodeProperties["Severity"] = new ParameterInfo { ParameterName = "Severity", ParameterType = typeof(Severity), DefaultParameterValue = _severity, ParameterCurrentValue = _severity, Description = "Severity", Choices = Enum.GetNames(typeof(Severity)) }; InvalidateVisual(); } } }
        public Likelihood Likelihood { get => _likelihood; set { if (_likelihood != value) { _likelihood = value; if (NodeProperties.TryGetValue("Likelihood", out var p)) p.ParameterCurrentValue = _likelihood; else NodeProperties["Likelihood"] = new ParameterInfo { ParameterName = "Likelihood", ParameterType = typeof(Likelihood), DefaultParameterValue = _likelihood, ParameterCurrentValue = _likelihood, Description = "Likelihood", Choices = Enum.GetNames(typeof(Likelihood)) }; InvalidateVisual(); } } }

        public ThreatNode()
        {
            Width = 140; Height = 80;
            NodeProperties["ThreatName"] = new ParameterInfo { ParameterName = "ThreatName", ParameterType = typeof(string), DefaultParameterValue = _threatName, ParameterCurrentValue = _threatName, Description = "Threat name" };
            NodeProperties["Severity"] = new ParameterInfo { ParameterName = "Severity", ParameterType = typeof(Severity), DefaultParameterValue = _severity, ParameterCurrentValue = _severity, Description = "Severity", Choices = Enum.GetNames(typeof(Severity)) };
            NodeProperties["Likelihood"] = new ParameterInfo { ParameterName = "Likelihood", ParameterType = typeof(Likelihood), DefaultParameterValue = _likelihood, ParameterCurrentValue = _likelihood, Description = "Likelihood", Choices = Enum.GetNames(typeof(Likelihood)) };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawSecurityContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);

            using var namePaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Edging = SKFontEdging.SubpixelAntialias, Embolden = true };
            using var metaPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8) { Edging = SKFontEdging.SubpixelAntialias };
            canvas.DrawText(ThreatName, r.MidX, r.MidY, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Severity} · {Likelihood}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
