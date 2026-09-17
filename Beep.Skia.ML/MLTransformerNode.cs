using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLTransformerNode : MLControl
    {
        private int _heads = 8;
        private int _layers = 6;
        private int _dModel = 512;
        private int _dFF = 2048;
        private double _dropout = 0.1;

        /// <summary>
        /// Gets or sets the attention heads.
        /// </summary>
        public int AttentionHeads { get => _heads; set { int v = Math.Max(1, value); if (_heads != v) { _heads = v; UpdateNodeProperty("AttentionHeads", _heads); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the layers.
        /// </summary>
        public int Layers { get => _layers; set { int v = Math.Max(1, value); if (_layers != v) { _layers = v; UpdateNodeProperty("Layers", _layers); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the model dim.
        /// </summary>
        public int ModelDim { get => _dModel; set { int v = Math.Max(1, value); if (_dModel != v) { _dModel = v; UpdateNodeProperty("ModelDim", _dModel); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the feed forward dim.
        /// </summary>
        public int FeedForwardDim { get => _dFF; set { int v = Math.Max(1, value); if (_dFF != v) { _dFF = v; UpdateNodeProperty("FeedForwardDim", _dFF); InvalidateVisual(); } } }
        /// <summary>
        /// Gets or sets the dropout.
        /// </summary>
        public double Dropout { get => _dropout; set { double v = Math.Clamp(value, 0, 0.9); if (Math.Abs(_dropout - v) > 0.001) { _dropout = v; UpdateNodeProperty("Dropout", _dropout); InvalidateVisual(); } } }

        /// <summary>
        /// Attention heads
        /// </summary>
        public MLTransformerNode()
        {
            Width = 145; Height = 85; Name = "Transformer";
            NodeProperties["AttentionHeads"] = new ParameterInfo { ParameterName = "AttentionHeads", ParameterType = typeof(int), DefaultParameterValue = _heads, ParameterCurrentValue = _heads, Description = "Attention heads" };
            NodeProperties["Layers"] = new ParameterInfo { ParameterName = "Layers", ParameterType = typeof(int), DefaultParameterValue = _layers, ParameterCurrentValue = _layers, Description = "Number of layers" };
            NodeProperties["ModelDim"] = new ParameterInfo { ParameterName = "ModelDim", ParameterType = typeof(int), DefaultParameterValue = _dModel, ParameterCurrentValue = _dModel, Description = "Model dimension" };
            NodeProperties["FeedForwardDim"] = new ParameterInfo { ParameterName = "FeedForwardDim", ParameterType = typeof(int), DefaultParameterValue = _dFF, ParameterCurrentValue = _dFF, Description = "Feed-forward dim" };
            NodeProperties["Dropout"] = new ParameterInfo { ParameterName = "Dropout", ParameterType = typeof(double), DefaultParameterValue = _dropout, ParameterCurrentValue = _dropout, Description = "Dropout rate" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Transformer", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_heads} heads, {_layers} layers", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            canvas.DrawText($"d={_dModel}", r.MidX, r.Bottom - 10, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
