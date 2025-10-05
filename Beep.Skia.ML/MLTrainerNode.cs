using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLTrainerNode : MLControl
    {
        private int _epochs = 100;
        private int _batchSize = 32;
        private double _learningRate = 0.001;
        private string _lossFunction = "CrossEntropy";
        private bool _earlyStopping = true;
        private int _patience = 10;

        public int Epochs { get => _epochs; set { int v = Math.Max(1, value); if (_epochs != v) { _epochs = v; UpdateNodeProperty("Epochs", _epochs); InvalidateVisual(); } } }
        public int BatchSize { get => _batchSize; set { int v = Math.Max(1, value); if (_batchSize != v) { _batchSize = v; UpdateNodeProperty("BatchSize", _batchSize); InvalidateVisual(); } } }
        public double LearningRate { get => _learningRate; set { double v = Math.Max(0.0001, value); if (Math.Abs(_learningRate - v) > 0.00001) { _learningRate = v; UpdateNodeProperty("LearningRate", _learningRate); InvalidateVisual(); } } }
        public string LossFunction { get => _lossFunction; set { var v = value ?? ""; if (_lossFunction != v) { _lossFunction = v; UpdateNodeProperty("LossFunction", _lossFunction); InvalidateVisual(); } } }
        public bool EarlyStopping { get => _earlyStopping; set { if (_earlyStopping != value) { _earlyStopping = value; UpdateNodeProperty("EarlyStopping", _earlyStopping); InvalidateVisual(); } } }
        public int Patience { get => _patience; set { int v = Math.Max(1, value); if (_patience != v) { _patience = v; UpdateNodeProperty("Patience", _patience); InvalidateVisual(); } } }

        public MLTrainerNode()
        {
            Width = 140; Height = 90; Name = "Trainer";
            NodeProperties["Epochs"] = new ParameterInfo { ParameterName = "Epochs", ParameterType = typeof(int), DefaultParameterValue = _epochs, ParameterCurrentValue = _epochs, Description = "Training epochs" };
            NodeProperties["BatchSize"] = new ParameterInfo { ParameterName = "BatchSize", ParameterType = typeof(int), DefaultParameterValue = _batchSize, ParameterCurrentValue = _batchSize, Description = "Batch size" };
            NodeProperties["LearningRate"] = new ParameterInfo { ParameterName = "LearningRate", ParameterType = typeof(double), DefaultParameterValue = _learningRate, ParameterCurrentValue = _learningRate, Description = "Learning rate" };
            NodeProperties["LossFunction"] = new ParameterInfo { ParameterName = "LossFunction", ParameterType = typeof(string), DefaultParameterValue = _lossFunction, ParameterCurrentValue = _lossFunction, Description = "Loss function", Choices = new[] { "CrossEntropy", "MSE", "MAE", "Hinge", "KLDiv" } };
            NodeProperties["EarlyStopping"] = new ParameterInfo { ParameterName = "EarlyStopping", ParameterType = typeof(bool), DefaultParameterValue = _earlyStopping, ParameterCurrentValue = _earlyStopping, Description = "Early stopping" };
            NodeProperties["Patience"] = new ParameterInfo { ParameterName = "Patience", ParameterType = typeof(int), DefaultParameterValue = _patience, ParameterCurrentValue = _patience, Description = "Patience epochs" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Trainer", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_epochs} epochs, LR={_learningRate}", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            canvas.DrawText(_lossFunction, r.MidX, r.Bottom - 10, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
