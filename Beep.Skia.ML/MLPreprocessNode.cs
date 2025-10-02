using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public enum TransformKind { Normalize, OneHot, Impute, Scale, PCA }

    public class MLPreprocessNode : MLControl
    {
        private string _name = "Preprocess";
        private TransformKind _transform = TransformKind.Normalize;
        private string _parameters = string.Empty;

        public string StepName { get => _name; set { var v = value ?? string.Empty; if (_name != v) { _name = v; if (NodeProperties.TryGetValue("StepName", out var p)) p.ParameterCurrentValue = _name; else NodeProperties["StepName"] = new ParameterInfo { ParameterName = "StepName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Step name" }; Name = _name; InvalidateVisual(); } } }
        public TransformKind Transform { get => _transform; set { if (_transform != value) { _transform = value; if (NodeProperties.TryGetValue("Transform", out var p)) p.ParameterCurrentValue = _transform; else NodeProperties["Transform"] = new ParameterInfo { ParameterName = "Transform", ParameterType = typeof(TransformKind), DefaultParameterValue = _transform, ParameterCurrentValue = _transform, Description = "Transform kind", Choices = Enum.GetNames(typeof(TransformKind)) }; InvalidateVisual(); } } }
        public string Parameters { get => _parameters; set { var v = value ?? string.Empty; if (_parameters != v) { _parameters = v; if (NodeProperties.TryGetValue("Parameters", out var p)) p.ParameterCurrentValue = _parameters; else NodeProperties["Parameters"] = new ParameterInfo { ParameterName = "Parameters", ParameterType = typeof(string), DefaultParameterValue = _parameters, ParameterCurrentValue = _parameters, Description = "Parameters" }; InvalidateVisual(); } } }

        public MLPreprocessNode()
        {
            Width = 170; Height = 90;
            NodeProperties["StepName"] = new ParameterInfo { ParameterName = "StepName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Step name" };
            NodeProperties["Transform"] = new ParameterInfo { ParameterName = "Transform", ParameterType = typeof(TransformKind), DefaultParameterValue = _transform, ParameterCurrentValue = _transform, Description = "Transform kind", Choices = Enum.GetNames(typeof(TransformKind)) };
            NodeProperties["Parameters"] = new ParameterInfo { ParameterName = "Parameters", ParameterType = typeof(string), DefaultParameterValue = _parameters, ParameterCurrentValue = _parameters, Description = "Parameters" };
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
            canvas.DrawText(StepName, r.MidX, r.MidY - 8, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Transform}", r.MidX, r.Bottom - 18, SKTextAlign.Center, metaFont, metaPaint);
            canvas.DrawText(Parameters, r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
