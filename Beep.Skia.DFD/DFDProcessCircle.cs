using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process (Yourdon/DeMarco): circular process node with one input and one output.
    /// </summary>
    public class DFDProcessCircle : DFDControl
    {
        public DFDProcessCircle()
        {
            Name = "Process (Circle)";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Place ports precisely on the circle perimeter; avoid extreme top/bottom by insetting 30% radius
            var r = Bounds;
            float radius = System.Math.Min(r.Width, r.Height) / 2f;
            float inset = radius * 0.3f;
            LayoutPortsOnEllipse(topInset: inset, bottomInset: inset, outwardOffset: 2f);
        }

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float d = System.Math.Min(r.Width, r.Height);
            var center = new SKPoint(r.MidX, r.MidY);
            float radius = d / 2f;

            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawCircle(center, radius, fill);
            canvas.DrawCircle(center, radius, stroke);

            DrawPorts(canvas);
        }
    }
}
