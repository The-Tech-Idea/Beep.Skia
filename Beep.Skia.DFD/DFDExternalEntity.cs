using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD External Entity: rectangle node.
    /// </summary>
    public class DFDExternalEntity : DFDControl
    {
        public DFDExternalEntity()
        {
            Name = "External";
            DisplayText = "External";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Simple rectangle: keep ports off the exact corners
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            LayoutPorts();

            var r = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            DrawPorts(canvas);
        }
    }
}
