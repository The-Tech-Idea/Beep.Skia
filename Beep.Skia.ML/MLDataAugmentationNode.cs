using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLDataAugmentationNode : MLControl
    {
        private string _augmentationType = "Image";
        private double _intensity = 0.5;
        private bool _randomFlip = true;
        private bool _randomRotation = true;
        private bool _randomCrop = false;

        public string AugmentationType { get => _augmentationType; set { var v = value ?? ""; if (_augmentationType != v) { _augmentationType = v; UpdateNodeProperty("AugmentationType", _augmentationType); InvalidateVisual(); } } }
        public double Intensity { get => _intensity; set { double v = Math.Clamp(value, 0, 1); if (Math.Abs(_intensity - v) > 0.001) { _intensity = v; UpdateNodeProperty("Intensity", _intensity); InvalidateVisual(); } } }
        public bool RandomFlip { get => _randomFlip; set { if (_randomFlip != value) { _randomFlip = value; UpdateNodeProperty("RandomFlip", _randomFlip); InvalidateVisual(); } } }
        public bool RandomRotation { get => _randomRotation; set { if (_randomRotation != value) { _randomRotation = value; UpdateNodeProperty("RandomRotation", _randomRotation); InvalidateVisual(); } } }
        public bool RandomCrop { get => _randomCrop; set { if (_randomCrop != value) { _randomCrop = value; UpdateNodeProperty("RandomCrop", _randomCrop); InvalidateVisual(); } } }

        public MLDataAugmentationNode()
        {
            Width = 155; Height = 85; Name = "Data Augmentation";
            NodeProperties["AugmentationType"] = new ParameterInfo { ParameterName = "AugmentationType", ParameterType = typeof(string), DefaultParameterValue = _augmentationType, ParameterCurrentValue = _augmentationType, Description = "Augmentation type", Choices = new[] { "Image", "Text", "Audio", "TabularNoise" } };
            NodeProperties["Intensity"] = new ParameterInfo { ParameterName = "Intensity", ParameterType = typeof(double), DefaultParameterValue = _intensity, ParameterCurrentValue = _intensity, Description = "Augmentation intensity" };
            NodeProperties["RandomFlip"] = new ParameterInfo { ParameterName = "RandomFlip", ParameterType = typeof(bool), DefaultParameterValue = _randomFlip, ParameterCurrentValue = _randomFlip, Description = "Random flip" };
            NodeProperties["RandomRotation"] = new ParameterInfo { ParameterName = "RandomRotation", ParameterType = typeof(bool), DefaultParameterValue = _randomRotation, ParameterCurrentValue = _randomRotation, Description = "Random rotation" };
            NodeProperties["RandomCrop"] = new ParameterInfo { ParameterName = "RandomCrop", ParameterType = typeof(bool), DefaultParameterValue = _randomCrop, ParameterCurrentValue = _randomCrop, Description = "Random crop" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Data Augmentation", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_augmentationType} ({_intensity:P0})", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
