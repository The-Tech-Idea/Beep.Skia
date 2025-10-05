using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Sequential Access Storage node: elongated oval for sequential access storage devices.
    /// Used for devices like magnetic tape where data must be accessed sequentially.
    /// </summary>
    public class SequentialAccessNode : FlowchartControl
    {
        private string _label = "Sequential Access";
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

        public SequentialAccessNode()
        {
            Name = "Flowchart Sequential Access";
            Width = 180;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Sequential access storage description."
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

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE1, 0xF5, 0xFE), IsAntialias = true }; // Light blue
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x03, 0xA9, 0xF4), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Blue
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Elongated oval (rounded rectangle with large corner radius)
            float radius = r.Height / 2;
            canvas.DrawRoundRect(r, radius, radius, fill);
            canvas.DrawRoundRect(r, radius, radius, stroke);

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
