using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLCrossValidationNode : MLControl
    {
        private int _folds = 5;
        private string _strategy = "KFold";
        private bool _shuffle = true;
        private int _randomSeed = 42;

        public int Folds { get => _folds; set { int v = Math.Max(2, value); if (_folds != v) { _folds = v; UpdateNodeProperty("Folds", _folds); InvalidateVisual(); } } }
        public string Strategy { get => _strategy; set { var v = value ?? ""; if (_strategy != v) { _strategy = v; UpdateNodeProperty("Strategy", _strategy); InvalidateVisual(); } } }
        public bool Shuffle { get => _shuffle; set { if (_shuffle != value) { _shuffle = value; UpdateNodeProperty("Shuffle", _shuffle); InvalidateVisual(); } } }
        public int RandomSeed { get => _randomSeed; set { if (_randomSeed != value) { _randomSeed = value; UpdateNodeProperty("RandomSeed", _randomSeed); InvalidateVisual(); } } }

        public MLCrossValidationNode()
        {
            Width = 150; Height = 80; Name = "Cross Validation";
            NodeProperties["Folds"] = new ParameterInfo { ParameterName = "Folds", ParameterType = typeof(int), DefaultParameterValue = _folds, ParameterCurrentValue = _folds, Description = "Number of folds" };
            NodeProperties["Strategy"] = new ParameterInfo { ParameterName = "Strategy", ParameterType = typeof(string), DefaultParameterValue = _strategy, ParameterCurrentValue = _strategy, Description = "CV strategy", Choices = new[] { "KFold", "Stratified", "TimeSeriesSplit", "LeaveOneOut" } };
            NodeProperties["Shuffle"] = new ParameterInfo { ParameterName = "Shuffle", ParameterType = typeof(bool), DefaultParameterValue = _shuffle, ParameterCurrentValue = _shuffle, Description = "Shuffle data" };
            NodeProperties["RandomSeed"] = new ParameterInfo { ParameterName = "RandomSeed", ParameterType = typeof(int), DefaultParameterValue = _randomSeed, ParameterCurrentValue = _randomSeed, Description = "Random seed" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Cross Validation", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_folds}-fold {_strategy}", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
