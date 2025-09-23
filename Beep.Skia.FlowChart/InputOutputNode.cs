using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Input/Output (parallelogram) node with slanted sides and centered label.
    /// </summary>
    public class InputOutputNode : FlowchartControl
    {
        public string Label { get; set; } = "Input/Output";

        public InputOutputNode()
        {
            Name = "Flowchart Input/Output";
            Width = 160;
            Height = 70;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;
            float slant = 16f;
            // Left vertical edge is slanted top-left; right edge slanted bottom-right
            var a = new SKPoint(b.Left + slant, b.Top);
            var b1 = new SKPoint(b.Right, b.Top);
            var c = new SKPoint(b.Right - slant, b.Bottom);
            var d = new SKPoint(b.Left, b.Bottom);
            // In along left slanted edge (d->a). Out along right slanted edge (b1->c)
            PlacePortsOnSegment(InConnectionPoints, d, a, outwardSign: -1f);
            PlacePortsOnSegment(OutConnectionPoints, b1, c, outwardSign: +1f);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var b = Bounds;
            float slant = 16f;
            using var fill = new SKPaint { Color = new SKColor(0xF3, 0xE5, 0xF5), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x8E, 0x24, 0xAA), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(b.Left + slant, b.Top);
            path.LineTo(b.Right, b.Top);
            path.LineTo(b.Right - slant, b.Bottom);
            path.LineTo(b.Left, b.Bottom);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            var tx = b.MidX - font.MeasureText(Label, text) / 2;
            var ty = b.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
