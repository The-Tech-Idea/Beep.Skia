using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Internal Storage node: rectangle with vertical lines for internal memory/storage.
    /// Used for data structures in memory (arrays, buffers, caches).
    /// </summary>
    public class InternalStorageNode : FlowchartControl
    {
        private string _label = "Internal Storage";
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

        public InternalStorageNode()
        {
            Name = "Flowchart Internal Storage";
            Width = 160;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Internal storage description."
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
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true }; // Light green
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x43, 0xA0, 0x47), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Green
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Draw rectangle
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Draw vertical divider lines (left side)
            float leftDivider = r.Left + r.Width * 0.15f;
            canvas.DrawLine(leftDivider, r.Top, leftDivider, r.Bottom, stroke);

            // Draw horizontal divider line (top)
            float topDivider = r.Top + r.Height * 0.25f;
            canvas.DrawLine(r.Left, topDivider, r.Right, topDivider, stroke);

            // Draw label in main area (right-center)
            float textX = leftDivider + 10;
            float textY = (topDivider + r.Bottom) / 2 + 5;
            canvas.DrawText(Label, textX, textY, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
