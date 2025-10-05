using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLInferenceNode : MLControl
    {
        private string _mode = "RealTime";
        private int _batchSize = 1;
        private bool _optimize = true;
        private double _confidenceThreshold = 0.5;

        public string Mode { get => _mode; set { var v = value ?? ""; if (_mode != v) { _mode = v; UpdateNodeProperty("Mode", _mode); InvalidateVisual(); } } }
        public int BatchSize { get => _batchSize; set { int v = Math.Max(1, value); if (_batchSize != v) { _batchSize = v; UpdateNodeProperty("BatchSize", _batchSize); InvalidateVisual(); } } }
        public bool Optimize { get => _optimize; set { if (_optimize != value) { _optimize = value; UpdateNodeProperty("Optimize", _optimize); InvalidateVisual(); } } }
        public double ConfidenceThreshold { get => _confidenceThreshold; set { double v = Math.Clamp(value, 0, 1); if (Math.Abs(_confidenceThreshold - v) > 0.001) { _confidenceThreshold = v; UpdateNodeProperty("ConfidenceThreshold", _confidenceThreshold); InvalidateVisual(); } } }

        public MLInferenceNode()
        {
            Width = 130; Height = 85; Name = "Inference";
            NodeProperties["Mode"] = new ParameterInfo { ParameterName = "Mode", ParameterType = typeof(string), DefaultParameterValue = _mode, ParameterCurrentValue = _mode, Description = "Inference mode", Choices = new[] { "RealTime", "Batch", "Streaming", "Edge" } };
            NodeProperties["BatchSize"] = new ParameterInfo { ParameterName = "BatchSize", ParameterType = typeof(int), DefaultParameterValue = _batchSize, ParameterCurrentValue = _batchSize, Description = "Batch size" };
            NodeProperties["Optimize"] = new ParameterInfo { ParameterName = "Optimize", ParameterType = typeof(bool), DefaultParameterValue = _optimize, ParameterCurrentValue = _optimize, Description = "Optimize inference" };
            NodeProperties["ConfidenceThreshold"] = new ParameterInfo { ParameterName = "ConfidenceThreshold", ParameterType = typeof(double), DefaultParameterValue = _confidenceThreshold, ParameterCurrentValue = _confidenceThreshold, Description = "Confidence threshold" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Inference", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_mode} (batch={_batchSize})", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
