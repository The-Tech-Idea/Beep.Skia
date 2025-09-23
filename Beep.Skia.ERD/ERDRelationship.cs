using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// ERD Relationship: diamond shape with ports on left/right, optional degree text.
    /// </summary>
    public class ERDRelationship : ERDControl
    {
        public string Label { get; set; } = "relates";
        public string Degree { get; set; } = "1..*";
        public bool Identifying { get; set; } = false;

        public ERDRelationship()
        {
            Name = "ERD Relationship";
            DisplayText = string.Empty;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Place single input/output at mid-left and mid-right
            var b = Bounds;
            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                cp.Center = new SKPoint(b.Left - 2, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cp.Center.X - PortRadius, cp.Center.Y - PortRadius, cp.Center.X + PortRadius, cp.Center.Y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0; cp.Component = this; cp.IsAvailable = true;
            }
            if (OutConnectionPoints.Count > 0)
            {
                var cp = OutConnectionPoints[0];
                cp.Center = new SKPoint(b.Right + 2, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cp.Center.X - PortRadius, cp.Center.Y - PortRadius, cp.Center.X + PortRadius, cp.Center.Y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0; cp.Component = this; cp.IsAvailable = true;
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var b = Bounds;
            var pTop = new SKPoint(b.MidX, b.Top);
            var pRight = new SKPoint(b.Right, b.MidY);
            var pBottom = new SKPoint(b.MidX, b.Bottom);
            var pLeft = new SKPoint(b.Left, b.MidY);

            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF8, 0xE1), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFB, 0x8C, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = Identifying ? 3 : 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 13);

            using var path = new SKPath();
            path.MoveTo(pTop);
            path.LineTo(pRight);
            path.LineTo(pBottom);
            path.LineTo(pLeft);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Centered label
            var label = string.IsNullOrEmpty(Label) ? "" : Label;
            var lx = b.MidX - font.MeasureText(label, text) / 2;
            var ly = b.MidY + 5;
            canvas.DrawText(label, lx, ly, SKTextAlign.Left, font, text);

            // Degree near top edge
            if (!string.IsNullOrEmpty(Degree))
            {
                var dx = b.MidX - font.MeasureText(Degree, text) / 2;
                var dy = b.Top + 16;
                canvas.DrawText(Degree, dx, dy, SKTextAlign.Left, font, text);
            }

            DrawPorts(canvas);
        }
    }
}
