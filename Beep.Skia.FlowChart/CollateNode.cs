using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Collate node: inverted trapezoid for sorting or collating data.
    /// Used to represent organizing or ordering operations.
    /// </summary>
    public class CollateNode : FlowchartControl
    {
        private string _label = "Collate";
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v, System.StringComparison.Ordinal))
                {
                    _label = v;
                    if (NodeProperties.TryGetValue("Label", out var pi))
                        pi.ParameterCurrentValue = _label;
                    InvalidateVisual();
                }
            }
        }

        public CollateNode()
        {
            Name = "Flowchart Collate";
            Width = 120;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the collate shape."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float slant = r.Width * 0.2f;

            // Inverted trapezoid: narrower at top, wider at bottom
            var points = new SKPoint[]
            {
                new SKPoint(r.Left + slant, r.Top),      // Top left
                new SKPoint(r.Right - slant, r.Top),     // Top right
                new SKPoint(r.Right, r.Bottom),          // Bottom right
                new SKPoint(r.Left, r.Bottom)            // Bottom left
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF3, 0xE5, 0xF5), IsAntialias = true }; // Light purple
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x9C, 0x27, 0xB0), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Purple
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i]);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
