using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public enum MLFramework { Sklearn, TensorFlow, PyTorch, XGBoost, LightGBM, Other }
    public enum ModelType { Classifier, Regressor, Clustering, Anomaly, NLP, Vision }

    public class MLModelNode : MLControl
    {
        private string _modelName = "Model";
        private MLFramework _framework = MLFramework.Sklearn;
        private ModelType _type = ModelType.Classifier;
        private string _hyperParams = "";

        public string ModelName { get => _modelName; set { var v = value ?? string.Empty; if (_modelName != v) { _modelName = v; if (NodeProperties.TryGetValue("ModelName", out var p)) p.ParameterCurrentValue = _modelName; else NodeProperties["ModelName"] = new ParameterInfo { ParameterName = "ModelName", ParameterType = typeof(string), DefaultParameterValue = _modelName, ParameterCurrentValue = _modelName, Description = "Model name" }; Name = _modelName; InvalidateVisual(); } } }
        public MLFramework Framework { get => _framework; set { if (_framework != value) { _framework = value; if (NodeProperties.TryGetValue("Framework", out var p)) p.ParameterCurrentValue = _framework; else NodeProperties["Framework"] = new ParameterInfo { ParameterName = "Framework", ParameterType = typeof(MLFramework), DefaultParameterValue = _framework, ParameterCurrentValue = _framework, Description = "Framework", Choices = Enum.GetNames(typeof(MLFramework)) }; InvalidateVisual(); } } }
        public ModelType ModelType { get => _type; set { if (_type != value) { _type = value; if (NodeProperties.TryGetValue("ModelType", out var p)) p.ParameterCurrentValue = _type; else NodeProperties["ModelType"] = new ParameterInfo { ParameterName = "ModelType", ParameterType = typeof(ModelType), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Model type", Choices = Enum.GetNames(typeof(ModelType)) }; InvalidateVisual(); } } }
        public string HyperParameters { get => _hyperParams; set { var v = value ?? string.Empty; if (_hyperParams != v) { _hyperParams = v; if (NodeProperties.TryGetValue("HyperParameters", out var p)) p.ParameterCurrentValue = _hyperParams; else NodeProperties["HyperParameters"] = new ParameterInfo { ParameterName = "HyperParameters", ParameterType = typeof(string), DefaultParameterValue = _hyperParams, ParameterCurrentValue = _hyperParams, Description = "Hyper parameters" }; InvalidateVisual(); } } }

        public MLModelNode()
        {
            Width = 140; Height = 90;
            NodeProperties["ModelName"] = new ParameterInfo { ParameterName = "ModelName", ParameterType = typeof(string), DefaultParameterValue = _modelName, ParameterCurrentValue = _modelName, Description = "Model name" };
            NodeProperties["Framework"] = new ParameterInfo { ParameterName = "Framework", ParameterType = typeof(MLFramework), DefaultParameterValue = _framework, ParameterCurrentValue = _framework, Description = "Framework", Choices = Enum.GetNames(typeof(MLFramework)) };
            NodeProperties["ModelType"] = new ParameterInfo { ParameterName = "ModelType", ParameterType = typeof(ModelType), DefaultParameterValue = _type, ParameterCurrentValue = _type, Description = "Model type", Choices = Enum.GetNames(typeof(ModelType)) };
            NodeProperties["HyperParameters"] = new ParameterInfo { ParameterName = "HyperParameters", ParameterType = typeof(string), DefaultParameterValue = _hyperParams, ParameterCurrentValue = _hyperParams, Description = "Hyper parameters" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);

            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(ModelName, r.MidX, r.MidY - 4, SKTextAlign.Center, nameFont, text);
            canvas.DrawText($"{Framework} · {ModelType}", r.MidX, r.MidY + 12, SKTextAlign.Center, metaFont, text);
            if (!string.IsNullOrEmpty(HyperParameters))
                canvas.DrawText(HyperParameters, r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, text);

            // Ports
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
