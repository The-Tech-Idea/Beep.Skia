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
