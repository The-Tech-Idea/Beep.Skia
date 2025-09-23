using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Data Store: rectangle with double vertical line on the left.
    /// </summary>
    public class DFDDataStore : DFDControl
    {
        public DFDDataStore()
        {
            Name = "Data Store";
            DisplayText = "Data Store";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Slight insets to avoid corners; move inputs further left to clear the double line
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f, leftOffset: -8f, rightOffset: 2f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            using var line = new SKPaint { Color = stroke.Color, IsAntialias = true, StrokeWidth = 2 };
            canvas.DrawLine(r.Left + 6, r.Top, r.Left + 6, r.Bottom, line);

            DrawPorts(canvas);
        }
    }
}
