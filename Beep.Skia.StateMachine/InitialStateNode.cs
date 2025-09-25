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
        public string Title { get; set; } = "Initial";

        public InitialStateNode()
        {
            Width = 40; Height = 40;
            BackgroundColor = MaterialColors.Primary;
            BorderColor = MaterialColors.Primary;
            TextColor = MaterialColors.OnPrimary;
            EnsurePortCounts(0, 1);
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

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
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
