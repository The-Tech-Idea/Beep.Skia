using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process (Gane & Sarson): rectangular process with header band for ID/numbering.
    /// </summary>
    public class DFDProcessGaneSarson : DFDControl
    {
        private const float HeaderHeight = 22f;

        public DFDProcessGaneSarson()
        {
            Name = "Process (Gane & Sarson)";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Avoid exact corners; small insets suffice
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE0, 0xF7, 0xFA), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x00, 0x96, 0x88), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var headerFill = new SKPaint { Color = new SKColor(0xB2, 0xEB, 0xF2), IsAntialias = true };

            // Body
            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            // Header band across the top
            var headerRect = new SKRect(r.Left, r.Top, r.Right, r.Top + HeaderHeight);
            canvas.DrawRect(headerRect, headerFill);
            canvas.DrawLine(headerRect.Left, headerRect.Bottom, headerRect.Right, headerRect.Bottom, stroke);

            DrawPorts(canvas);
        }
    }
}
