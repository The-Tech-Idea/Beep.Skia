using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public class RiskAssessmentNode : SecurityControl
    {
        private string _assessmentName = "Risk Assessment";
        private double _riskScore = 0.5; // 0..1
        private DateTime _assessedOn = DateTime.Today;

        public string AssessmentName { get => _assessmentName; set { var v = value ?? string.Empty; if (_assessmentName != v) { _assessmentName = v; if (NodeProperties.TryGetValue("AssessmentName", out var p)) p.ParameterCurrentValue = _assessmentName; else NodeProperties["AssessmentName"] = new ParameterInfo { ParameterName = "AssessmentName", ParameterType = typeof(string), DefaultParameterValue = _assessmentName, ParameterCurrentValue = _assessmentName, Description = "Assessment name" }; Name = _assessmentName; InvalidateVisual(); } } }
        public double RiskScore { get => _riskScore; set { var v = Math.Max(0, Math.Min(1, value)); if (Math.Abs(_riskScore - v) > double.Epsilon) { _riskScore = v; if (NodeProperties.TryGetValue("RiskScore", out var p)) p.ParameterCurrentValue = _riskScore; else NodeProperties["RiskScore"] = new ParameterInfo { ParameterName = "RiskScore", ParameterType = typeof(double), DefaultParameterValue = _riskScore, ParameterCurrentValue = _riskScore, Description = "Risk score (0..1)" }; InvalidateVisual(); } } }
        public DateTime AssessedOn { get => _assessedOn; set { if (_assessedOn != value) { _assessedOn = value; if (NodeProperties.TryGetValue("AssessedOn", out var p)) p.ParameterCurrentValue = _assessedOn; else NodeProperties["AssessedOn"] = new ParameterInfo { ParameterName = "AssessedOn", ParameterType = typeof(DateTime), DefaultParameterValue = _assessedOn, ParameterCurrentValue = _assessedOn, Description = "Assessment date" }; InvalidateVisual(); } } }

        public RiskAssessmentNode()
        {
            Width = 180; Height = 90;
            NodeProperties["AssessmentName"] = new ParameterInfo { ParameterName = "AssessmentName", ParameterType = typeof(string), DefaultParameterValue = _assessmentName, ParameterCurrentValue = _assessmentName, Description = "Assessment name" };
            NodeProperties["RiskScore"] = new ParameterInfo { ParameterName = "RiskScore", ParameterType = typeof(double), DefaultParameterValue = _riskScore, ParameterCurrentValue = _riskScore, Description = "Risk score (0..1)" };
            NodeProperties["AssessedOn"] = new ParameterInfo { ParameterName = "AssessedOn", ParameterType = typeof(DateTime), DefaultParameterValue = _assessedOn, ParameterCurrentValue = _assessedOn, Description = "Assessment date" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawSecurityContent(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);

            // risk meter bar
            var meterRect = new SKRect(r.Left + 10, r.MidY + 8, r.Right - 10, r.MidY + 18);
            using var meterBg = new SKPaint { Color = new SKColor(220, 220, 220), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var meterFg = new SKPaint { Color = MaterialColors.Error, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRoundRect(meterRect, 3, 3, meterBg);
            var w = (float)(meterRect.Width * Math.Max(0, Math.Min(1, RiskScore)));
            var fgRect = new SKRect(meterRect.Left, meterRect.Top, meterRect.Left + w, meterRect.Bottom);
            canvas.DrawRoundRect(fgRect, 3, 3, meterFg);

            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(AssessmentName, r.MidX, r.MidY - 6, SKTextAlign.Center, nameFont, text);
            canvas.DrawText($"Score: {RiskScore:0.00} · {AssessedOn:yyyy-MM-dd}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, text);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
