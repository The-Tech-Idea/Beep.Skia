using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Data Store (Yourdon/DeMarco): two parallel horizontal lines.
    /// </summary>
    public class DFDDataStoreParallel : DFDControl
    {
        public DFDDataStoreParallel()
        {
            Name = "Data Store (Parallel)";
            DisplayText = "Data Store";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Keep ports between the two inner horizontal lines: compute dynamic insets from bounds
            var r = Bounds;
            float inset = Math.Max(8f, Math.Min(r.Height * 0.2f, 20f));
            LayoutPortsVerticalSegments(topInset: inset, bottomInset: inset, leftOffset: -2f, rightOffset: 2f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            LayoutPorts();

            var r = Bounds;
            using var stroke = new SKPaint { Color = new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var faint = new SKPaint { Color = stroke.Color.WithAlpha(50), IsAntialias = true, Style = SKPaintStyle.Fill };

            // Slight background tint
            canvas.DrawRect(r, faint);

            // Two horizontal lines inside bounds
            float inset = 10f;
            float y1 = r.Top + inset;
            float y2 = r.Bottom - inset;
            canvas.DrawLine(r.Left, y1, r.Right, y1, stroke);
            canvas.DrawLine(r.Left, y2, r.Right, y2, stroke);

            DrawPorts(canvas);
        }
    }
}
