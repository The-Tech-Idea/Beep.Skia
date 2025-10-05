using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Direct Access Storage node: cylinder with flat bottom for random access storage (hard disk).
    /// </summary>
    public class DirectAccessNode : FlowchartControl
    {
        private string _label = "Direct Access";
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

        public DirectAccessNode()
        {
            Name = "Flowchart Direct Access Storage";
            Width = 140;
            Height = 100;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Direct access storage description."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 10f, bottomInset: 6f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float ellipseHeight = r.Height * 0.15f;

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF3, 0xE5, 0xF5), IsAntialias = true }; // Light purple
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x9C, 0x27, 0xB0), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Purple
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Top ellipse
            var topEllipse = new SKRect(r.Left, r.Top, r.Right, r.Top + ellipseHeight * 2);
            
            // Side rectangles
            var leftRect = new SKRect(r.Left, r.Top + ellipseHeight, r.Left + r.Width * 0.1f, r.Bottom);
            var rightRect = new SKRect(r.Right - r.Width * 0.1f, r.Top + ellipseHeight, r.Right, r.Bottom);
            var centerRect = new SKRect(r.Left, r.Top + ellipseHeight, r.Right, r.Bottom);

            // Draw center body
            canvas.DrawRect(centerRect, fill);

            // Draw side rectangles (darker for 3D effect)
            using var darkFill = new SKPaint { Color = CustomFillColor != null ? CustomFillColor.Value.WithAlpha((byte)(CustomFillColor.Value.Alpha * 0.8f)) : new SKColor(0xE1, 0xBE, 0xE7), IsAntialias = true };
            canvas.DrawRect(leftRect, darkFill);
            canvas.DrawRect(rightRect, darkFill);

            // Draw top ellipse
            canvas.DrawOval(topEllipse, fill);
            canvas.DrawOval(topEllipse, stroke);

            // Draw side lines
            canvas.DrawLine(r.Left, r.Top + ellipseHeight, r.Left, r.Bottom, stroke);
            canvas.DrawLine(r.Right, r.Top + ellipseHeight, r.Right, r.Bottom, stroke);

            // Draw bottom line
            canvas.DrawLine(r.Left, r.Bottom, r.Right, r.Bottom, stroke);

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
