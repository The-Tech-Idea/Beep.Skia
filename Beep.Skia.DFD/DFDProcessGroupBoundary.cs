using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// Process grouping boundary with a distinctive dash pattern; no ports.
    /// </summary>
    public class DFDProcessGroupBoundary : DFDControl
    {
        public DFDProcessGroupBoundary()
        {
            Name = "Process Group Boundary";
            DisplayText = "Group";
            TextPosition = Beep.Skia.TextPosition.Above;
            EnsurePortCounts(0, 0);
        }

        protected override void LayoutPorts() { /* no ports */ }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            var rect = Bounds;
            using var stroke = new SKPaint
            {
                Color = new SKColor(0x45, 0x65, 0xA4),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 3, 4, 8, 4 }, 0)
            };
            using var fill = new SKPaint { Color = new SKColor(0x42, 0x85, 0xF4, 0x18), IsAntialias = true };

            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);
        }
    }
}
