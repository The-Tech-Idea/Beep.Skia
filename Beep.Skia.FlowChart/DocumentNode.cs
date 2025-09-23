using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Document: rectangle with a curved bottom edge and centered label.
    /// </summary>
    public class DocumentNode : FlowchartControl
    {
        public string Label { get; set; } = "Document";
        // Optionally place outgoing ports along the top straight edge (avoids curved bottom)
        public bool OutPortsOnTop { get; set; } = false;

        public DocumentNode()
        {
            Name = "Flowchart Document";
            Width = 160;
            Height = 80;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;
            // Favor straight edges for stable attachment: left and right verticals excluding curved bottom area
            PlacePortsAlongVerticalEdge(InConnectionPoints, b.Left, b.Top + 8f, b.Bottom - 18f, outwardSign: -1f);
            if (OutPortsOnTop)
            {
                PlacePortsAlongHorizontalEdge(OutConnectionPoints, b.Top, b.Left + 12f, b.Right - 12f, outwardSign: -1f);
            }
            else
            {
                PlacePortsAlongVerticalEdge(OutConnectionPoints, b.Right, b.Top + 8f, b.Bottom - 18f, outwardSign: +1f);
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var b = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xEB, 0xEE), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xC2, 0x18, 0x5B), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            float curve = 12f;
            path.MoveTo(b.Left, b.Top);
            path.LineTo(b.Right, b.Top);
            path.LineTo(b.Right, b.Bottom - curve);
            path.CubicTo(b.Right - 30, b.Bottom + curve, b.Left + 30, b.Bottom + curve, b.Left, b.Bottom - curve);
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
