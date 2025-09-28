using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Milestone: diamond shape.
    /// </summary>
    public class MilestoneNode : PMControl
    {
        /// <summary>
        /// The label drawn at the center of the milestone diamond.
        /// </summary>
        private string _label = "Milestone";
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v, System.StringComparison.Ordinal))
                {
                    _label = v;
                    InvalidateVisual();
                }
            }
        }

        public MilestoneNode()
        {
            Name = "PM Milestone";
            Width = 80;
            Height = 80;
            InPortCount = 1;
            OutPortCount = 1;
        }

        protected override void LayoutPorts()
        {
            // Diamond milestone: ports on middle left/right points of the diamond
            var b = Bounds;
            float cy = b.MidY;
            
            // Position input ports on the left point of diamond
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(b.Left, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cp.Center.X - PortRadius, cp.Center.Y - PortRadius, cp.Center.X + PortRadius, cp.Center.Y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
            
            // Position output ports on the right point of diamond
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(b.Right, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cp.Center.X - PortRadius, cp.Center.Y - PortRadius, cp.Center.X + PortRadius, cp.Center.Y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var r = Bounds;
            var cx = r.MidX; var cy = r.MidY;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 13);

            // Diamond points
            var top = new SKPoint(cx, r.Top);
            var right = new SKPoint(r.Right, cy);
            var bottom = new SKPoint(cx, r.Bottom);
            var left = new SKPoint(r.Left, cy);

            using var path = new SKPath();
            path.MoveTo(top);
            path.LineTo(right);
            path.LineTo(bottom);
            path.LineTo(left);
            path.Close();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            canvas.DrawText(Label, cx, cy + 5, SKTextAlign.Center, font, text);

            DrawPorts(canvas);
        }
    }
}
