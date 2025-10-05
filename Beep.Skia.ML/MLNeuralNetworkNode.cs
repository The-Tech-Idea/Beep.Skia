using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLNeuralNetworkNode : MLControl
    {
        private string _architecture = "Dense";
        private int _layers = 3;
        private string _layerSizes = "128,64,32";
        private string _activation = "ReLU";
        private double _dropout = 0.2;
        private string _optimizer = "Adam";

        public string Architecture { get => _architecture; set { var v = value ?? ""; if (_architecture != v) { _architecture = v; UpdateNodeProperty("Architecture", _architecture); InvalidateVisual(); } } }
        public int Layers { get => _layers; set { int v = Math.Max(1, value); if (_layers != v) { _layers = v; UpdateNodeProperty("Layers", _layers); InvalidateVisual(); } } }
        public string LayerSizes { get => _layerSizes; set { var v = value ?? ""; if (_layerSizes != v) { _layerSizes = v; UpdateNodeProperty("LayerSizes", _layerSizes); InvalidateVisual(); } } }
        public string Activation { get => _activation; set { var v = value ?? ""; if (_activation != v) { _activation = v; UpdateNodeProperty("Activation", _activation); InvalidateVisual(); } } }
        public double Dropout { get => _dropout; set { double v = Math.Clamp(value, 0, 0.9); if (Math.Abs(_dropout - v) > 0.001) { _dropout = v; UpdateNodeProperty("Dropout", _dropout); InvalidateVisual(); } } }
        public string Optimizer { get => _optimizer; set { var v = value ?? ""; if (_optimizer != v) { _optimizer = v; UpdateNodeProperty("Optimizer", _optimizer); InvalidateVisual(); } } }

        public MLNeuralNetworkNode()
        {
            Width = 150; Height = 90; Name = "Neural Network";
            NodeProperties["Architecture"] = new ParameterInfo { ParameterName = "Architecture", ParameterType = typeof(string), DefaultParameterValue = _architecture, ParameterCurrentValue = _architecture, Description = "Architecture", Choices = new[] { "Dense", "Residual", "Highway" } };
            NodeProperties["Layers"] = new ParameterInfo { ParameterName = "Layers", ParameterType = typeof(int), DefaultParameterValue = _layers, ParameterCurrentValue = _layers, Description = "Number of layers" };
            NodeProperties["LayerSizes"] = new ParameterInfo { ParameterName = "LayerSizes", ParameterType = typeof(string), DefaultParameterValue = _layerSizes, ParameterCurrentValue = _layerSizes, Description = "Layer sizes (comma-separated)" };
            NodeProperties["Activation"] = new ParameterInfo { ParameterName = "Activation", ParameterType = typeof(string), DefaultParameterValue = _activation, ParameterCurrentValue = _activation, Description = "Activation function", Choices = new[] { "ReLU", "Sigmoid", "Tanh", "LeakyReLU", "ELU" } };
            NodeProperties["Dropout"] = new ParameterInfo { ParameterName = "Dropout", ParameterType = typeof(double), DefaultParameterValue = _dropout, ParameterCurrentValue = _dropout, Description = "Dropout rate" };
            NodeProperties["Optimizer"] = new ParameterInfo { ParameterName = "Optimizer", ParameterType = typeof(string), DefaultParameterValue = _optimizer, ParameterCurrentValue = _optimizer, Description = "Optimizer", Choices = new[] { "Adam", "SGD", "RMSprop", "AdaGrad" } };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Neural Network", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_layers} layers", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            canvas.DrawText(_activation, r.MidX, r.Bottom - 10, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
