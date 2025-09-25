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
            using var fill = new SKPaint { Color = MaterialColors.SurfaceContainer, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            // Folded corner effect (top-right)
            float fold = 12f;
            using var foldPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            var path = new SKPath();
            path.MoveTo(r.Right - fold, r.Top);
            path.LineTo(r.Right, r.Top);
            path.LineTo(r.Right, r.Top + fold);
            path.Close();
            canvas.DrawPath(path, foldPaint);
        }
    }
}
