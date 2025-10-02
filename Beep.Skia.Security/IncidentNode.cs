using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum IncidentStatus { Open, Investigating, Contained, Resolved }

    public class IncidentNode : SecurityControl
    {
        private string _incidentId = "INC-0001";
        private IncidentStatus _status = IncidentStatus.Open;
        private DateTime _detectedOn = DateTime.Now;
        private Confidence _confidence = Confidence.Medium;

        public string IncidentId { get => _incidentId; set { var v = value ?? string.Empty; if (_incidentId != v) { _incidentId = v; if (NodeProperties.TryGetValue("IncidentId", out var p)) p.ParameterCurrentValue = _incidentId; else NodeProperties["IncidentId"] = new ParameterInfo { ParameterName = "IncidentId", ParameterType = typeof(string), DefaultParameterValue = _incidentId, ParameterCurrentValue = _incidentId, Description = "Incident ID" }; Name = _incidentId; InvalidateVisual(); } } }
        public IncidentStatus Status { get => _status; set { if (_status != value) { _status = value; if (NodeProperties.TryGetValue("Status", out var p)) p.ParameterCurrentValue = _status; else NodeProperties["Status"] = new ParameterInfo { ParameterName = "Status", ParameterType = typeof(IncidentStatus), DefaultParameterValue = _status, ParameterCurrentValue = _status, Description = "Incident status", Choices = Enum.GetNames(typeof(IncidentStatus)) }; InvalidateVisual(); } } }
        public DateTime DetectedOn { get => _detectedOn; set { if (_detectedOn != value) { _detectedOn = value; if (NodeProperties.TryGetValue("DetectedOn", out var p)) p.ParameterCurrentValue = _detectedOn; else NodeProperties["DetectedOn"] = new ParameterInfo { ParameterName = "DetectedOn", ParameterType = typeof(DateTime), DefaultParameterValue = _detectedOn, ParameterCurrentValue = _detectedOn, Description = "Detection timestamp" }; InvalidateVisual(); } } }
        public Confidence Confidence { get => _confidence; set { if (_confidence != value) { _confidence = value; if (NodeProperties.TryGetValue("Confidence", out var p)) p.ParameterCurrentValue = _confidence; else NodeProperties["Confidence"] = new ParameterInfo { ParameterName = "Confidence", ParameterType = typeof(Confidence), DefaultParameterValue = _confidence, ParameterCurrentValue = _confidence, Description = "Detection confidence", Choices = Enum.GetNames(typeof(Confidence)) }; InvalidateVisual(); } } }

        public IncidentNode()
        {
            Width = 180; Height = 90;
            NodeProperties["IncidentId"] = new ParameterInfo { ParameterName = "IncidentId", ParameterType = typeof(string), DefaultParameterValue = _incidentId, ParameterCurrentValue = _incidentId, Description = "Incident ID" };
            NodeProperties["Status"] = new ParameterInfo { ParameterName = "Status", ParameterType = typeof(IncidentStatus), DefaultParameterValue = _status, ParameterCurrentValue = _status, Description = "Incident status", Choices = Enum.GetNames(typeof(IncidentStatus)) };
            NodeProperties["DetectedOn"] = new ParameterInfo { ParameterName = "DetectedOn", ParameterType = typeof(DateTime), DefaultParameterValue = _detectedOn, ParameterCurrentValue = _detectedOn, Description = "Detection timestamp" };
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
            canvas.DrawText(IncidentId, r.MidX, r.MidY - 8, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Status} · {Confidence}", r.MidX, r.Bottom - 18, SKTextAlign.Center, metaFont, metaPaint);
            canvas.DrawText($"Detected: {DetectedOn:yyyy-MM-dd HH:mm}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
