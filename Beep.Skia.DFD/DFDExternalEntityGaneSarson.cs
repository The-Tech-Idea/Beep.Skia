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

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var header = new SKPaint { Color = MaterialColors.PrimaryContainer, IsAntialias = true };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            var headerRect = new SKRect(r.Left, r.Top, r.Right, r.Top + HeaderHeight);
            canvas.DrawRect(headerRect, header);
            canvas.DrawLine(headerRect.Left, headerRect.Bottom, headerRect.Right, headerRect.Bottom, stroke);

            DrawPorts(canvas);
        }
    }
}
