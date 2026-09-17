using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Fork pseudostate: one input, two or more outputs.
    /// Rendered as a thick horizontal bar splitting a single flow into multiple parallel flows.
    /// </summary>
    public class ForkNode : StateMachineControl
    {
        private string _title = "Fork";
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? string.Empty;
                if (_title == v) return;
                _title = v;
                if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title;
                InvalidateVisual();
            }
        }

        private int _outPortCount = 2;
        public new int OutPortCount
        {
            get => _outPortCount;
            set
            {
                var v = Math.Max(2, Math.Min(6, value));
                if (_outPortCount == v) return;
                _outPortCount = v;
                EnsurePortCounts(1, _outPortCount);
                if (NodeProperties.TryGetValue("OutPortCount", out var pi)) pi.ParameterCurrentValue = _outPortCount;
                InvalidateVisual();
            }
        }

        public ForkNode()
        {
            Width = 60; Height = 16;
            BackgroundColor = MaterialColors.Primary;
            BorderColor = MaterialColors.Primary;
            TextColor = MaterialColors.OnPrimary;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 2);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Fork label" };
            NodeProperties["OutPortCount"] = new ParameterInfo { ParameterName = "OutPortCount", ParameterType = typeof(int), DefaultParameterValue = _outPortCount, ParameterCurrentValue = _outPortCount, Description = "Number of output branches" };
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;

            // Single input on the left
            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                cp.Center = new SKPoint(b.Left, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(b.Left - PortRadius, b.MidY - PortRadius, b.Left + PortRadius, b.MidY + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            // Multiple outputs on the right, evenly spaced
            float spacing = b.Height / (OutConnectionPoints.Count + 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                var cp = OutConnectionPoints[i];
                float cy = b.Top + spacing * (i + 1);
                cp.Center = new SKPoint(b.Right, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(b.Right - PortRadius, cy - PortRadius, b.Right + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };

            canvas.DrawRoundRect(rect, 4, 4, fill);
            canvas.DrawRoundRect(rect, 4, 4, stroke);

            DrawConnectionPoints(canvas);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["OutPortCount"] = _outPortCount;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("OutPortCount", out var o) && o != null) OutPortCount = Convert.ToInt32(o);
        }
    }
}
