using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Manual Input node: parallelogram with top slanted (wider at top) for manual data entry.
    /// </summary>
    public class ManualInputNode : FlowchartControl
    {
        private string _label = "Manual Input";
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

        public ManualInputNode()
        {
            Name = "Flowchart Manual Input";
            Width = 160;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the manual input shape."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 12f, bottomInset: 6f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float topSlant = r.Height * 0.25f;

            // Parallelogram: top edge slants down from left to right
            var points = new SKPoint[]
            {
                new SKPoint(r.Left, r.Top + topSlant),      // Top left (lower)
                new SKPoint(r.Right, r.Top),                // Top right (higher)
                new SKPoint(r.Right, r.Bottom),             // Bottom right
                new SKPoint(r.Left, r.Bottom)               // Bottom left
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE1, 0xBE, 0xE7), IsAntialias = true }; // Light purple
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x8E, 0x24, 0xAA), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Purple
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
