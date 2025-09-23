using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process (SSADM): ellipse process node with one input and one output.
    /// </summary>
    public class DFDProcessOval : DFDControl
    {
        public DFDProcessOval()
        {
            Name = "Process (Oval)";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Inside;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Place ports precisely at ellipse perimeter intersections; generous vertical inset
            var r = Bounds;
            float inset = System.Math.Min(r.Width, r.Height) * 0.25f;
            LayoutPortsOnEllipse(inset, inset, outwardOffset: 2f);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            using var fill = new SKPaint { Color = new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x43, 0xA0, 0x47), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            canvas.DrawOval(Bounds, fill);
            canvas.DrawOval(Bounds, stroke);

            DrawPorts(canvas);
        }
    }
}
