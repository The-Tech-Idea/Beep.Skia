using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ML
{
    public class MLHyperparameterTuningNode : MLControl
    {
        private string _method = "GridSearch";
        private int _iterations = 50;
        private string _metric = "Accuracy";
        private bool _parallelExecution = true;

        public string Method { get => _method; set { var v = value ?? ""; if (_method != v) { _method = v; UpdateNodeProperty("Method", _method); InvalidateVisual(); } } }
        public int Iterations { get => _iterations; set { int v = Math.Max(1, value); if (_iterations != v) { _iterations = v; UpdateNodeProperty("Iterations", _iterations); InvalidateVisual(); } } }
        public string Metric { get => _metric; set { var v = value ?? ""; if (_metric != v) { _metric = v; UpdateNodeProperty("Metric", _metric); InvalidateVisual(); } } }
        public bool ParallelExecution { get => _parallelExecution; set { if (_parallelExecution != value) { _parallelExecution = value; UpdateNodeProperty("ParallelExecution", _parallelExecution); InvalidateVisual(); } } }

        public MLHyperparameterTuningNode()
        {
            Width = 170; Height = 85; Name = "Hyperparameter Tuning";
            NodeProperties["Method"] = new ParameterInfo { ParameterName = "Method", ParameterType = typeof(string), DefaultParameterValue = _method, ParameterCurrentValue = _method, Description = "Tuning method", Choices = new[] { "GridSearch", "RandomSearch", "BayesianOpt", "Genetic" } };
            NodeProperties["Iterations"] = new ParameterInfo { ParameterName = "Iterations", ParameterType = typeof(int), DefaultParameterValue = _iterations, ParameterCurrentValue = _iterations, Description = "Max iterations" };
            NodeProperties["Metric"] = new ParameterInfo { ParameterName = "Metric", ParameterType = typeof(string), DefaultParameterValue = _metric, ParameterCurrentValue = _metric, Description = "Optimization metric" };
            NodeProperties["ParallelExecution"] = new ParameterInfo { ParameterName = "ParallelExecution", ParameterType = typeof(bool), DefaultParameterValue = _parallelExecution, ParameterCurrentValue = _parallelExecution, Description = "Parallel execution" };
            EnsurePortCounts(2, 1);
        }

        protected override void DrawMLContent(SKCanvas canvas, DrawingContext context)
        {
            base.DrawMLContent(canvas, context);
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            canvas.DrawText("Hyperparameter Tuning", r.MidX, r.Top + 18, SKTextAlign.Center, font, text);
            using var small = new SKFont(SKTypeface.Default, 9);
            canvas.DrawText($"{_method} ({_iterations})", r.MidX, r.MidY + 5, SKTextAlign.Center, small, text);
            DrawPorts(canvas);
        }

        private void UpdateNodeProperty(string name, object value)
        {
            if (NodeProperties.TryGetValue(name, out var p)) p.ParameterCurrentValue = value;
        }
    }
}
