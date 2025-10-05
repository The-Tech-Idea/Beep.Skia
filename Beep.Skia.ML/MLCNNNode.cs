using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLCNNNode : MLControl
    {
        private int _convLayers = 3;
        private string _filters = "32,64,128";
        private string _kernelSize = "3x3";
        private string _pooling = "MaxPool";
        private bool _batchNorm = true;

        public int ConvLayers { get => _convLayers; set { int v = Math.Max(1, value); if (_convLayers != v) { _convLayers = v; UpdateNodeProperty("ConvLayers", _convLayers); InvalidateVisual(); } } }
        public string Filters { get => _filters; set { var v = value ?? ""; if (_filters != v) { _filters = v; UpdateNodeProperty("Filters", _filters); InvalidateVisual(); } } }
        public string KernelSize { get => _kernelSize; set { var v = value ?? ""; if (_kernelSize != v) { _kernelSize = v; UpdateNodeProperty("KernelSize", _kernelSize); InvalidateVisual(); } } }
        public string Pooling { get => _pooling; set { var v = value ?? ""; if (_pooling != v) { _pooling = v; UpdateNodeProperty("Pooling", _pooling); InvalidateVisual(); } } }
        public bool BatchNormalization { get => _batchNorm; set { if (_batchNorm != value) { _batchNorm = value; UpdateNodeProperty("BatchNormalization", _batchNorm); InvalidateVisual(); } } }

        public MLCNNNode()
        {
            Width = 140; Height = 85; Name = "CNN";
            NodeProperties["ConvLayers"] = new ParameterInfo { ParameterName = "ConvLayers", ParameterType = typeof(int), DefaultParameterValue = _convLayers, ParameterCurrentValue = _convLayers, Description = "Conv layers" };
            NodeProperties["Filters"] = new ParameterInfo { ParameterName = "Filters", ParameterType = typeof(string), DefaultParameterValue = _filters, ParameterCurrentValue = _filters, Description = "Filters per layer" };
            NodeProperties["KernelSize"] = new ParameterInfo { ParameterName = "KernelSize", ParameterType = typeof(string), DefaultParameterValue = _kernelSize, ParameterCurrentValue = _kernelSize, Description = "Kernel size", Choices = new[] { "3x3", "5x5", "7x7" } };
            NodeProperties["Pooling"] = new ParameterInfo { ParameterName = "Pooling", ParameterType = typeof(string), DefaultParameterValue = _pooling, ParameterCurrentValue = _pooling, Description = "Pooling type", Choices = new[] { "MaxPool", "AvgPool", "GlobalPool" } };
            NodeProperties["BatchNormalization"] = new ParameterInfo { ParameterName = "BatchNormalization", ParameterType = typeof(bool), DefaultParameterValue = _batchNorm, ParameterCurrentValue = _batchNorm, Description = "Batch normalization" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("CNN", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_convLayers} conv layers", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            canvas.DrawText(_pooling, r.MidX, r.Bottom - 10, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
