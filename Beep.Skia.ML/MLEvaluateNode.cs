using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public enum MetricKind { Accuracy, F1, Precision, Recall, RMSE, MAE }

    public class MLEvaluateNode : MLControl
    {
        private string _name = "Evaluate";
        private MetricKind _metric = MetricKind.Accuracy;
        private double _threshold = 0.9;

        public string StepName { get => _name; set { var v = value ?? string.Empty; if (_name != v) { _name = v; if (NodeProperties.TryGetValue("StepName", out var p)) p.ParameterCurrentValue = _name; else NodeProperties["StepName"] = new ParameterInfo { ParameterName = "StepName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Step name" }; Name = _name; InvalidateVisual(); } } }
        public MetricKind Metric { get => _metric; set { if (_metric != value) { _metric = value; if (NodeProperties.TryGetValue("Metric", out var p)) p.ParameterCurrentValue = _metric; else NodeProperties["Metric"] = new ParameterInfo { ParameterName = "Metric", ParameterType = typeof(MetricKind), DefaultParameterValue = _metric, ParameterCurrentValue = _metric, Description = "Metric kind", Choices = Enum.GetNames(typeof(MetricKind)) }; InvalidateVisual(); } } }
        public double Threshold { get => _threshold; set { var v = Math.Max(0, Math.Min(1, value)); if (Math.Abs(_threshold - v) > double.Epsilon) { _threshold = v; if (NodeProperties.TryGetValue("Threshold", out var p)) p.ParameterCurrentValue = _threshold; else NodeProperties["Threshold"] = new ParameterInfo { ParameterName = "Threshold", ParameterType = typeof(double), DefaultParameterValue = _threshold, ParameterCurrentValue = _threshold, Description = "Threshold (0..1)" }; InvalidateVisual(); } } }

        public MLEvaluateNode()
        {
            Width = 160; Height = 80;
            NodeProperties["StepName"] = new ParameterInfo { ParameterName = "StepName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Step name" };
            NodeProperties["Metric"] = new ParameterInfo { ParameterName = "Metric", ParameterType = typeof(MetricKind), DefaultParameterValue = _metric, ParameterCurrentValue = _metric, Description = "Metric kind", Choices = Enum.GetNames(typeof(MetricKind)) };
            NodeProperties["Threshold"] = new ParameterInfo { ParameterName = "Threshold", ParameterType = typeof(double), DefaultParameterValue = _threshold, ParameterCurrentValue = _threshold, Description = "Threshold (0..1)" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, IsAntialias = true, Style = SKPaintStyle.Stroke };
            canvas.DrawRoundRect(r, 8, 8, fill);
            canvas.DrawRoundRect(r, 8, 8, border);

            using var namePaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var metaPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(StepName, r.MidX, r.MidY - 6, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Metric} · thr {Threshold:0.00}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
