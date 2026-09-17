using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Data Store: open-ended rectangle (no right border) variant.
    /// </summary>
    public class DFDDataStoreOpenEnded : DFDControl
    {
        /// <summary>
        /// Initializes a new instance of the DFDDataStoreOpenEnded class.
        /// </summary>
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

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            // Background
            canvas.DrawRect(r, fill);

            // Draw three-sided border: left, top, bottom
            using var pathBuilder = new SKPathBuilder();
            pathBuilder.MoveTo(r.Left, r.Top);
            pathBuilder.LineTo(r.Right, r.Top);   // top
            pathBuilder.MoveTo(r.Left, r.Bottom);
            pathBuilder.LineTo(r.Right, r.Bottom); // bottom
            pathBuilder.MoveTo(r.Left, r.Top);
            pathBuilder.LineTo(r.Left, r.Bottom); // left
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, stroke);

            DrawPorts(canvas);
        }
    }
}
