using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLFeatureEngineeringNode : MLControl
    {
        private string _operation = "Polynomial";
        private int _degree = 2;
        private bool _interactions = true;
        private string _encoding = "Auto";

        public string Operation { get => _operation; set { var v = value ?? ""; if (_operation != v) { _operation = v; UpdateNodeProperty("Operation", _operation); InvalidateVisual(); } } }
        public int Degree { get => _degree; set { int v = Math.Max(1, Math.Min(5, value)); if (_degree != v) { _degree = v; UpdateNodeProperty("Degree", _degree); InvalidateVisual(); } } }
        public bool Interactions { get => _interactions; set { if (_interactions != value) { _interactions = value; UpdateNodeProperty("Interactions", _interactions); InvalidateVisual(); } } }
        public string Encoding { get => _encoding; set { var v = value ?? ""; if (_encoding != v) { _encoding = v; UpdateNodeProperty("Encoding", _encoding); InvalidateVisual(); } } }

        public MLFeatureEngineeringNode()
        {
            Width = 160; Height = 80; Name = "Feature Engineering";
            NodeProperties["Operation"] = new ParameterInfo { ParameterName = "Operation", ParameterType = typeof(string), DefaultParameterValue = _operation, ParameterCurrentValue = _operation, Description = "Feature operation", Choices = new[] { "Polynomial", "Binning", "Aggregate", "Embedding" } };
            NodeProperties["Degree"] = new ParameterInfo { ParameterName = "Degree", ParameterType = typeof(int), DefaultParameterValue = _degree, ParameterCurrentValue = _degree, Description = "Polynomial degree" };
            NodeProperties["Interactions"] = new ParameterInfo { ParameterName = "Interactions", ParameterType = typeof(bool), DefaultParameterValue = _interactions, ParameterCurrentValue = _interactions, Description = "Include interactions" };
            NodeProperties["Encoding"] = new ParameterInfo { ParameterName = "Encoding", ParameterType = typeof(string), DefaultParameterValue = _encoding, ParameterCurrentValue = _encoding, Description = "Categorical encoding", Choices = new[] { "Auto", "OneHot", "Label", "Target", "Ordinal" } };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Feature Engineering", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_operation} (deg={_degree})", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
