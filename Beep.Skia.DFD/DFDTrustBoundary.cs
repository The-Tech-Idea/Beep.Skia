using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Trust Boundary: dashed rounded rectangle container; no ports.
    /// </summary>
    public class DFDTrustBoundary : DFDControl
    {
        public DFDTrustBoundary()
        {
            Name = "Trust Boundary";
            DisplayText = "Trust Boundary";
            TextPosition = Beep.Skia.TextPosition.Above;
            // No connection points for a boundary
            EnsurePortCounts(0, 0);
        }

        protected override void LayoutPorts()
        {
            // Intentionally no ports
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var rect = Bounds;
            using var stroke = new SKPaint
            {
                Color = new SKColor(0x90, 0xA4, 0xAE), // blue-grey
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 8, 6 }, 0)
            };
            using var fill = new SKPaint { Color = new SKColor(0xE0, 0xE0, 0xE0, 0x20), IsAntialias = true };

            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);
        }
    }
}
