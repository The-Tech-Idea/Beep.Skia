using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLBatchPredictionNode : MLControl
    {
        private int _batchSize = 32;
        private bool _parallel = true;
        private int _maxWorkers = 4;
        private string _outputFormat = "JSON";

        public int BatchSize { get => _batchSize; set { int v = Math.Max(1, value); if (_batchSize != v) { _batchSize = v; UpdateNodeProperty("BatchSize", _batchSize); InvalidateVisual(); } } }
        public bool Parallel { get => _parallel; set { if (_parallel != value) { _parallel = value; UpdateNodeProperty("Parallel", _parallel); InvalidateVisual(); } } }
        public int MaxWorkers { get => _maxWorkers; set { int v = Math.Max(1, Math.Min(16, value)); if (_maxWorkers != v) { _maxWorkers = v; UpdateNodeProperty("MaxWorkers", _maxWorkers); InvalidateVisual(); } } }
        public string OutputFormat { get => _outputFormat; set { var v = value ?? ""; if (_outputFormat != v) { _outputFormat = v; UpdateNodeProperty("OutputFormat", _outputFormat); InvalidateVisual(); } } }

        public MLBatchPredictionNode()
        {
            Width = 145; Height = 85; Name = "Batch Prediction";
            NodeProperties["BatchSize"] = new ParameterInfo { ParameterName = "BatchSize", ParameterType = typeof(int), DefaultParameterValue = _batchSize, ParameterCurrentValue = _batchSize, Description = "Batch size" };
            NodeProperties["Parallel"] = new ParameterInfo { ParameterName = "Parallel", ParameterType = typeof(bool), DefaultParameterValue = _parallel, ParameterCurrentValue = _parallel, Description = "Parallel processing" };
            NodeProperties["MaxWorkers"] = new ParameterInfo { ParameterName = "MaxWorkers", ParameterType = typeof(int), DefaultParameterValue = _maxWorkers, ParameterCurrentValue = _maxWorkers, Description = "Max workers" };
            NodeProperties["OutputFormat"] = new ParameterInfo { ParameterName = "OutputFormat", ParameterType = typeof(string), DefaultParameterValue = _outputFormat, ParameterCurrentValue = _outputFormat, Description = "Output format", Choices = new[] { "JSON", "CSV", "Parquet", "Arrow" } };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Batch Prediction", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"Batch={_batchSize}, Workers={_maxWorkers}", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
