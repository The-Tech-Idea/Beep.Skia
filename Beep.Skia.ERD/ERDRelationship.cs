using SkiaSharp;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// ERD Relationship: diamond shape with ports on left/right, optional degree text.
    /// </summary>
    public class ERDRelationship : ERDControl
    {
        private string _label = "relates";
        public string Label { get => _label; set { if (_label == value) return; _label = value ?? string.Empty; InvalidateVisual(); } }
        private string _degree = "1..*";
        public string Degree { get => _degree; set { if (_degree == value) return; _degree = value ?? string.Empty; InvalidateVisual(); } }
        private bool _identifying = false;
        public bool Identifying { get => _identifying; set { if (_identifying == value) return; _identifying = value; InvalidateVisual(); } }

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

            using var fill = new SKPaint { Color = MaterialControl.MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialControl.MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = Identifying ? 3 : 1.5f };
            using var text = new SKPaint { Color = MaterialControl.MaterialColors.OnSurface, IsAntialias = true };
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
