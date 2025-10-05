using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Display node: rounded rectangle with curved bottom (like a monitor) for output display.
    /// </summary>
    public class DisplayNode : FlowchartControl
    {
        private string _label = "Display";
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

        public DisplayNode()
        {
            Name = "Flowchart Display";
            Width = 140;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the display symbol."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 10f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE0, 0xF7, 0xFA), IsAntialias = true }; // Light cyan
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x00, 0xBC, 0xD4), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Cyan
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            // Monitor shape: straight top and sides, curved bottom
            float curveDepth = r.Height * 0.15f;
            
            path.MoveTo(r.Left, r.Top);
            path.LineTo(r.Right, r.Top);
            path.LineTo(r.Right, r.Bottom - curveDepth);
            
            // Curved bottom
            path.CubicTo(
                r.Right, r.Bottom,
                r.Left, r.Bottom,
                r.Left, r.Bottom - curveDepth
            );
            
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
