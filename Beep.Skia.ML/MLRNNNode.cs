using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLRNNNode : MLControl
    {
        private string _cellType = "LSTM";
        private int _layers = 2;
        private int _hiddenSize = 128;
        private bool _bidirectional = false;
        private double _dropout = 0.2;

        public string CellType { get => _cellType; set { var v = value ?? ""; if (_cellType != v) { _cellType = v; UpdateNodeProperty("CellType", _cellType); InvalidateVisual(); } } }
        public int Layers { get => _layers; set { int v = Math.Max(1, value); if (_layers != v) { _layers = v; UpdateNodeProperty("Layers", _layers); InvalidateVisual(); } } }
        public int HiddenSize { get => _hiddenSize; set { int v = Math.Max(1, value); if (_hiddenSize != v) { _hiddenSize = v; UpdateNodeProperty("HiddenSize", _hiddenSize); InvalidateVisual(); } } }
        public bool Bidirectional { get => _bidirectional; set { if (_bidirectional != value) { _bidirectional = value; UpdateNodeProperty("Bidirectional", _bidirectional); InvalidateVisual(); } } }
        public double Dropout { get => _dropout; set { double v = Math.Clamp(value, 0, 0.9); if (Math.Abs(_dropout - v) > 0.001) { _dropout = v; UpdateNodeProperty("Dropout", _dropout); InvalidateVisual(); } } }

        public MLRNNNode()
        {
            Width = 130; Height = 85; Name = "RNN";
            NodeProperties["CellType"] = new ParameterInfo { ParameterName = "CellType", ParameterType = typeof(string), DefaultParameterValue = _cellType, ParameterCurrentValue = _cellType, Description = "Cell type", Choices = new[] { "LSTM", "GRU", "RNN" } };
            NodeProperties["Layers"] = new ParameterInfo { ParameterName = "Layers", ParameterType = typeof(int), DefaultParameterValue = _layers, ParameterCurrentValue = _layers, Description = "Number of layers" };
            NodeProperties["HiddenSize"] = new ParameterInfo { ParameterName = "HiddenSize", ParameterType = typeof(int), DefaultParameterValue = _hiddenSize, ParameterCurrentValue = _hiddenSize, Description = "Hidden size" };
            NodeProperties["Bidirectional"] = new ParameterInfo { ParameterName = "Bidirectional", ParameterType = typeof(bool), DefaultParameterValue = _bidirectional, ParameterCurrentValue = _bidirectional, Description = "Bidirectional" };
            NodeProperties["Dropout"] = new ParameterInfo { ParameterName = "Dropout", ParameterType = typeof(double), DefaultParameterValue = _dropout, ParameterCurrentValue = _dropout, Description = "Dropout rate" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("RNN", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_cellType} x{_layers}", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            canvas.DrawText($"h={_hiddenSize}", r.MidX, r.Bottom - 10, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
