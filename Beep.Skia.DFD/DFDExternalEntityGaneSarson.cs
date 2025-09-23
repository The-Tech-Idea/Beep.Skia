using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD External Entity (Gane & Sarson): rectangle with a header band.
    /// </summary>
    public class DFDExternalEntityGaneSarson : DFDControl
    {
        private const float HeaderHeight = 20f;

        public DFDExternalEntityGaneSarson()
        {
            Name = "External (Gane & Sarson)";
            DisplayText = "External";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Keep off corners and header band
            LayoutPortsVerticalSegments(topInset: HeaderHeight + 6f, bottomInset: 8f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x1E, 0x88, 0xE5), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var header = new SKPaint { Color = new SKColor(0xBB, 0xDE, 0xFB), IsAntialias = true };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            var headerRect = new SKRect(r.Left, r.Top, r.Right, r.Top + HeaderHeight);
            canvas.DrawRect(headerRect, header);
            canvas.DrawLine(headerRect.Left, headerRect.Bottom, headerRect.Right, headerRect.Bottom, stroke);

            DrawPorts(canvas);
        }
    }
}
