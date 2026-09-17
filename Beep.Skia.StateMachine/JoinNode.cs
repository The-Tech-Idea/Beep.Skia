using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Join pseudostate: two or more inputs, one output.
    /// Rendered as a thick horizontal bar merging multiple parallel flows into one.
    /// </summary>
    public class JoinNode : StateMachineControl
    {
        private string _title = "Join";
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

        private int _inPortCount = 2;
        public new int InPortCount
        {
            get => _inPortCount;
            set
            {
                var v = Math.Max(2, Math.Min(6, value));
                if (_inPortCount == v) return;
                _inPortCount = v;
                EnsurePortCounts(_inPortCount, 1);
                if (NodeProperties.TryGetValue("InPortCount", out var pi)) pi.ParameterCurrentValue = _inPortCount;
                InvalidateVisual();
            }
        }

        public JoinNode()
        {
            Width = 60; Height = 16;
            BackgroundColor = MaterialColors.Primary;
            BorderColor = MaterialColors.Primary;
            TextColor = MaterialColors.OnPrimary;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(2, 1);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Join label" };
            NodeProperties["InPortCount"] = new ParameterInfo { ParameterName = "InPortCount", ParameterType = typeof(int), DefaultParameterValue = _inPortCount, ParameterCurrentValue = _inPortCount, Description = "Number of input branches" };
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;

            // Multiple inputs on the left, evenly spaced
            float spacing = b.Height / (InConnectionPoints.Count + 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                var cp = InConnectionPoints[i];
                float cy = b.Top + spacing * (i + 1);
                cp.Center = new SKPoint(b.Left, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(b.Left - PortRadius, cy - PortRadius, b.Left + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            // Single output on the right
            if (OutConnectionPoints.Count > 0)
            {
                var cp = OutConnectionPoints[0];
                cp.Center = new SKPoint(b.Right, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(b.Right - PortRadius, b.MidY - PortRadius, b.Right + PortRadius, b.MidY + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
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
            props["InPortCount"] = _inPortCount;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("InPortCount", out var o) && o != null) InPortCount = Convert.ToInt32(o);
        }
    }
}
