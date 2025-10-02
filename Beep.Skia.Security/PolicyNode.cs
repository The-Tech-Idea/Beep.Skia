using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum PolicyStatus { Draft, Review, Approved, Deprecated }

    public class PolicyNode : SecurityControl
    {
        private string _policyName = "Policy";
        private string _version = "1.0";
        private PolicyStatus _status = PolicyStatus.Draft;

        public string PolicyName { get => _policyName; set { var v = value ?? string.Empty; if (_policyName != v) { _policyName = v; if (NodeProperties.TryGetValue("PolicyName", out var p)) p.ParameterCurrentValue = _policyName; else NodeProperties["PolicyName"] = new ParameterInfo { ParameterName = "PolicyName", ParameterType = typeof(string), DefaultParameterValue = _policyName, ParameterCurrentValue = _policyName, Description = "Policy name" }; Name = _policyName; InvalidateVisual(); } } }
        public string Version { get => _version; set { var v = value ?? string.Empty; if (_version != v) { _version = v; if (NodeProperties.TryGetValue("Version", out var p)) p.ParameterCurrentValue = _version; else NodeProperties["Version"] = new ParameterInfo { ParameterName = "Version", ParameterType = typeof(string), DefaultParameterValue = _version, ParameterCurrentValue = _version, Description = "Policy version" }; InvalidateVisual(); } } }
        public PolicyStatus Status { get => _status; set { if (_status != value) { _status = value; if (NodeProperties.TryGetValue("Status", out var p)) p.ParameterCurrentValue = _status; else NodeProperties["Status"] = new ParameterInfo { ParameterName = "Status", ParameterType = typeof(PolicyStatus), DefaultParameterValue = _status, ParameterCurrentValue = _status, Description = "Policy status", Choices = Enum.GetNames(typeof(PolicyStatus)) }; InvalidateVisual(); } } }

        public PolicyNode()
        {
            Width = 170; Height = 80;
            NodeProperties["PolicyName"] = new ParameterInfo { ParameterName = "PolicyName", ParameterType = typeof(string), DefaultParameterValue = _policyName, ParameterCurrentValue = _policyName, Description = "Policy name" };
            NodeProperties["Version"] = new ParameterInfo { ParameterName = "Version", ParameterType = typeof(string), DefaultParameterValue = _version, ParameterCurrentValue = _version, Description = "Policy version" };
            NodeProperties["Status"] = new ParameterInfo { ParameterName = "Status", ParameterType = typeof(PolicyStatus), DefaultParameterValue = _status, ParameterCurrentValue = _status, Description = "Policy status", Choices = Enum.GetNames(typeof(PolicyStatus)) };
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
            canvas.DrawText(PolicyName, r.MidX, r.MidY, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"v{Version} · {Status}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
