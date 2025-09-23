using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Data Store: open-ended rectangle (no right border) variant.
    /// </summary>
    public class DFDDataStoreOpenEnded : DFDControl
    {
        public DFDDataStoreOpenEnded()
        {
            Name = "Data Store (Open)";
            DisplayText = "Data Store";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Avoid corners; pull outputs slightly further right so they visually attach to the open side
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f, leftOffset: -2f, rightOffset: 6f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            // Background
            canvas.DrawRect(r, fill);

            // Draw three-sided border: left, top, bottom
            using var path = new SKPath();
            path.MoveTo(r.Left, r.Top);
            path.LineTo(r.Right, r.Top);   // top
            path.MoveTo(r.Left, r.Bottom);
            path.LineTo(r.Right, r.Bottom); // bottom
            path.MoveTo(r.Left, r.Top);
            path.LineTo(r.Left, r.Bottom); // left
            canvas.DrawPath(path, stroke);

            DrawPorts(canvas);
        }
    }
}
