using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Initial state: drawn as a filled circle, no inputs, one output.
    /// </summary>
    public class InitialStateNode : StateMachineControl
    {
        private string _title = "Initial";
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

        public InitialStateNode()
        {
            Width = 40; Height = 40;
            BackgroundColor = MaterialColors.Primary;
            BorderColor = MaterialColors.Primary;
            TextColor = MaterialColors.OnPrimary;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(0, 1);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Label" };
        }

        protected override void LayoutPorts()
        {
            // Place single output on the right center, no inputs
            EnsurePortCounts(0, OutConnectionPoints.Count);
            var b = Bounds;
            if (OutConnectionPoints.Count > 0)
            {
                var cp = OutConnectionPoints[0];
                float cx = b.Right + 2f;
                float cy = b.MidY;
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            float r = MathF.Min(Width, Height) / 2f;
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawCircle(cx, cy, r, fill);
            canvas.DrawCircle(cx, cy, r, stroke);
            DrawConnectionPoints(canvas);
        }
    }
}
