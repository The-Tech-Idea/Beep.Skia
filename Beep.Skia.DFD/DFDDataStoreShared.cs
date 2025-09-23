using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Shared Data Store: rectangle with double vertical lines on both left and right sides.
    /// </summary>
    public class DFDDataStoreShared : DFDControl
    {
        public DFDDataStoreShared()
        {
            Name = "Data Store (Shared)";
            DisplayText = "Shared Store";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Avoid corners; offset ports further from the decorated edges
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f, leftOffset: -8f, rightOffset: 8f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var deco = new SKPaint { Color = stroke.Color, IsAntialias = true, StrokeWidth = 2 };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            // Double vertical lines left and right
            float inset = 6f;
            canvas.DrawLine(r.Left + inset, r.Top, r.Left + inset, r.Bottom, deco);
            canvas.DrawLine(r.Right - inset, r.Top, r.Right - inset, r.Bottom, deco);

            DrawPorts(canvas);
        }
    }
}
