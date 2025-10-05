using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLModelLoadNode : MLControl
    {
        private string _modelPath = "./model.onnx";
        private string _framework = "ONNX";
        private string _device = "CPU";
        private bool _validateModel = true;

        public string ModelPath { get => _modelPath; set { var v = value ?? ""; if (_modelPath != v) { _modelPath = v; UpdateNodeProperty("ModelPath", _modelPath); InvalidateVisual(); } } }
        public string Framework { get => _framework; set { var v = value ?? ""; if (_framework != v) { _framework = v; UpdateNodeProperty("Framework", _framework); InvalidateVisual(); } } }
        public string Device { get => _device; set { var v = value ?? ""; if (_device != v) { _device = v; UpdateNodeProperty("Device", _device); InvalidateVisual(); } } }
        public bool ValidateModel { get => _validateModel; set { if (_validateModel != value) { _validateModel = value; UpdateNodeProperty("ValidateModel", _validateModel); InvalidateVisual(); } } }

        public MLModelLoadNode()
        {
            Width = 130; Height = 80; Name = "Model Load";
            NodeProperties["ModelPath"] = new ParameterInfo { ParameterName = "ModelPath", ParameterType = typeof(string), DefaultParameterValue = _modelPath, ParameterCurrentValue = _modelPath, Description = "Model path" };
            NodeProperties["Framework"] = new ParameterInfo { ParameterName = "Framework", ParameterType = typeof(string), DefaultParameterValue = _framework, ParameterCurrentValue = _framework, Description = "Framework", Choices = new[] { "ONNX", "TorchScript", "SavedModel", "PMML", "CoreML" } };
            NodeProperties["Device"] = new ParameterInfo { ParameterName = "Device", ParameterType = typeof(string), DefaultParameterValue = _device, ParameterCurrentValue = _device, Description = "Device", Choices = new[] { "CPU", "GPU", "TPU" } };
            NodeProperties["ValidateModel"] = new ParameterInfo { ParameterName = "ValidateModel", ParameterType = typeof(bool), DefaultParameterValue = _validateModel, ParameterCurrentValue = _validateModel, Description = "Validate model" };
            EnsurePortCounts(0, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Model Load", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_framework} ({_device})", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
