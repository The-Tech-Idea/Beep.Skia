using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process: rounded rectangle node with one input and one output.
    /// </summary>
    public class DFDProcess : DFDControl
    {
        public DFDProcess()
        {
            Name = "Process";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Avoid rounded corners by starting/ending at corner radius
            LayoutPortsVerticalSegments(topInset: CornerRadius, bottomInset: CornerRadius);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            LayoutPorts();

            var rect = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            DrawPorts(canvas);
        }
    }
}
