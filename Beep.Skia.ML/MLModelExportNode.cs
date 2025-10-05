using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLModelExportNode : MLControl
    {
        private string _format = "ONNX";
        private string _outputPath = "./model";
        private bool _compression = false;
        private bool _includeMetadata = true;

        public string Format { get => _format; set { var v = value ?? ""; if (_format != v) { _format = v; UpdateNodeProperty("Format", _format); InvalidateVisual(); } } }
        public string OutputPath { get => _outputPath; set { var v = value ?? ""; if (_outputPath != v) { _outputPath = v; UpdateNodeProperty("OutputPath", _outputPath); InvalidateVisual(); } } }
        public bool Compression { get => _compression; set { if (_compression != value) { _compression = value; UpdateNodeProperty("Compression", _compression); InvalidateVisual(); } } }
        public bool IncludeMetadata { get => _includeMetadata; set { if (_includeMetadata != value) { _includeMetadata = value; UpdateNodeProperty("IncludeMetadata", _includeMetadata); InvalidateVisual(); } } }

        public MLModelExportNode()
        {
            Width = 140; Height = 80; Name = "Model Export";
            NodeProperties["Format"] = new ParameterInfo { ParameterName = "Format", ParameterType = typeof(string), DefaultParameterValue = _format, ParameterCurrentValue = _format, Description = "Export format", Choices = new[] { "ONNX", "TorchScript", "SavedModel", "PMML", "CoreML" } };
            NodeProperties["OutputPath"] = new ParameterInfo { ParameterName = "OutputPath", ParameterType = typeof(string), DefaultParameterValue = _outputPath, ParameterCurrentValue = _outputPath, Description = "Output path" };
            NodeProperties["Compression"] = new ParameterInfo { ParameterName = "Compression", ParameterType = typeof(bool), DefaultParameterValue = _compression, ParameterCurrentValue = _compression, Description = "Compress model" };
            NodeProperties["IncludeMetadata"] = new ParameterInfo { ParameterName = "IncludeMetadata", ParameterType = typeof(bool), DefaultParameterValue = _includeMetadata, ParameterCurrentValue = _includeMetadata, Description = "Include metadata" };
            EnsurePortCounts(1, 0);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Model Export", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText(_format, r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
