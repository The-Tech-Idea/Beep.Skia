using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Stored data node: parallelogram tilted to the right for data storage (tape, disk).
    /// Different from InputOutput which tilts both sides.
    /// </summary>
    public class StoredDataNode : FlowchartControl
    {
        private string _label = "Stored Data";
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

        public StoredDataNode()
        {
            Name = "Flowchart Stored Data";
            Width = 160;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the stored data symbol."
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
            float slant = r.Height * 0.2f;

            // Parallelogram tilted right (curved left edge for tape/disk appearance)
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF1, 0xF8, 0xE9), IsAntialias = true }; // Light green
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x66, 0x9B, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Olive green
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            // Left edge is curved (like tape reel)
            path.MoveTo(r.Left + slant * 0.3f, r.Top);
            path.CubicTo(
                r.Left - slant * 0.2f, r.Top + r.Height * 0.3f,
                r.Left - slant * 0.2f, r.Top + r.Height * 0.7f,
                r.Left + slant * 0.3f, r.Bottom
            );
            
            // Bottom edge
            path.LineTo(r.Right, r.Bottom);
            
            // Right edge (straight)
            path.LineTo(r.Right, r.Top);
            
            // Top edge
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
