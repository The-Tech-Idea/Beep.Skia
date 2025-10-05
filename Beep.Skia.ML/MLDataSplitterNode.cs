using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLDataSplitterNode : MLControl
    {
        private double _trainRatio = 0.7;
        private double _validationRatio = 0.15;
        private double _testRatio = 0.15;
        private bool _shuffle = true;
        private int _randomSeed = 42;
        private bool _stratify = false;

        public double TrainRatio { get => _trainRatio; set { double v = Math.Clamp(value, 0.1, 0.9); if (Math.Abs(_trainRatio - v) > 0.001) { _trainRatio = v; UpdateNodeProperty("TrainRatio", _trainRatio); InvalidateVisual(); } } }
        public double ValidationRatio { get => _validationRatio; set { double v = Math.Clamp(value, 0, 0.5); if (Math.Abs(_validationRatio - v) > 0.001) { _validationRatio = v; UpdateNodeProperty("ValidationRatio", _validationRatio); InvalidateVisual(); } } }
        public double TestRatio { get => _testRatio; set { double v = Math.Clamp(value, 0.05, 0.5); if (Math.Abs(_testRatio - v) > 0.001) { _testRatio = v; UpdateNodeProperty("TestRatio", _testRatio); InvalidateVisual(); } } }
        public bool Shuffle { get => _shuffle; set { if (_shuffle != value) { _shuffle = value; UpdateNodeProperty("Shuffle", _shuffle); InvalidateVisual(); } } }
        public int RandomSeed { get => _randomSeed; set { if (_randomSeed != value) { _randomSeed = value; UpdateNodeProperty("RandomSeed", _randomSeed); InvalidateVisual(); } } }
        public bool Stratify { get => _stratify; set { if (_stratify != value) { _stratify = value; UpdateNodeProperty("Stratify", _stratify); InvalidateVisual(); } } }

        public MLDataSplitterNode()
        {
            Width = 140; Height = 90; Name = "Data Splitter";
            NodeProperties["TrainRatio"] = new ParameterInfo { ParameterName = "TrainRatio", ParameterType = typeof(double), DefaultParameterValue = _trainRatio, ParameterCurrentValue = _trainRatio, Description = "Train ratio" };
            NodeProperties["ValidationRatio"] = new ParameterInfo { ParameterName = "ValidationRatio", ParameterType = typeof(double), DefaultParameterValue = _validationRatio, ParameterCurrentValue = _validationRatio, Description = "Validation ratio" };
            NodeProperties["TestRatio"] = new ParameterInfo { ParameterName = "TestRatio", ParameterType = typeof(double), DefaultParameterValue = _testRatio, ParameterCurrentValue = _testRatio, Description = "Test ratio" };
            NodeProperties["Shuffle"] = new ParameterInfo { ParameterName = "Shuffle", ParameterType = typeof(bool), DefaultParameterValue = _shuffle, ParameterCurrentValue = _shuffle, Description = "Shuffle data" };
            NodeProperties["RandomSeed"] = new ParameterInfo { ParameterName = "RandomSeed", ParameterType = typeof(int), DefaultParameterValue = _randomSeed, ParameterCurrentValue = _randomSeed, Description = "Random seed" };
            NodeProperties["Stratify"] = new ParameterInfo { ParameterName = "Stratify", ParameterType = typeof(bool), DefaultParameterValue = _stratify, ParameterCurrentValue = _stratify, Description = "Stratify split" };
            EnsurePortCounts(1, 3);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Data Splitter", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_trainRatio:P0}/{_validationRatio:P0}/{_testRatio:P0}", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
