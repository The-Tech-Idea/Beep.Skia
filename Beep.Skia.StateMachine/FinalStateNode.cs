using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Final state: double circle (outer ring), one input, no outputs.
    /// </summary>
    public class FinalStateNode : StateMachineControl
    {
        private string _title = "Final";
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

        public FinalStateNode()
        {
            Width = 46; Height = 46;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Primary;
            TextColor = MaterialColors.OnSurface;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 0);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Label" };
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(InConnectionPoints.Count, 0);
            // Single input on the left center
            var b = Bounds;
            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                float cx = b.Left - 2f;
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
            float r = MathF.Min(Width, Height) / 2f - 2f;
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawCircle(cx, cy, r, fill);
            canvas.DrawCircle(cx, cy, r, stroke);
            // outer ring
            canvas.DrawCircle(cx, cy, r + 5f, stroke);
            DrawConnectionPoints(canvas);
        }
    }
}
