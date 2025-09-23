using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Note: informational sticky-note; no ports.
    /// </summary>
    public class DFDNote : DFDControl
    {
        public DFDNote()
        {
            Name = "Note";
            DisplayText = "Note";
            TextPosition = Beep.Skia.TextPosition.Inside;
            EnsurePortCounts(0, 0);
        }

        protected override void LayoutPorts()
        {
            // Note does not have ports
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF9, 0xC4), IsAntialias = true }; // light yellow
            using var stroke = new SKPaint { Color = new SKColor(0xF9, 0xA8, 0x25), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            // Folded corner effect (top-right)
            float fold = 12f;
            using var foldPaint = new SKPaint { Color = new SKColor(0xFF, 0xEE, 0x58), IsAntialias = true };
            var path = new SKPath();
            path.MoveTo(r.Right - fold, r.Top);
            path.LineTo(r.Right, r.Top);
            path.LineTo(r.Right, r.Top + fold);
            path.Close();
            canvas.DrawPath(path, foldPaint);
        }
    }
}
