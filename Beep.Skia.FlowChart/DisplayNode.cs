using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Display node: rounded rectangle with curved bottom (like a monitor) for output display.
    /// </summary>
    public class DisplayNode : DirectedFlowchartControl
    {
        private string _label = "Display";
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
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

        /// <summary>
        /// Text shown inside the display symbol.
        /// </summary>
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

        protected override void LayoutSidePorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 10f);
        }

        /// <summary>How far the curved bottom edge dips below its ends.</summary>
        private static float CurveDepth(SKRect r) => r.Height * 0.15f;

        /// <summary>The curved bottom edge, from its right end to its left end: both ends and the two control points.</summary>
        private static (SKPoint End, SKPoint Control1, SKPoint Control2, SKPoint Start) BottomCurve(SKRect r)
        {
            var depth = CurveDepth(r);
            return (new(r.Right, r.Bottom - depth), new(r.Right, r.Bottom), new(r.Left, r.Bottom), new(r.Left, r.Bottom - depth));
        }

        /// <summary>Inputs along the flat top, outputs along the curved bottom.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var (right, toRight, toLeft, left) = BottomCurve(r);
            PlaceAlongEdge(InConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, x => CubicYAt(right, toRight, toLeft, left, x), +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE0, 0xF7, 0xFA), IsAntialias = true }; // Light cyan
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x00, 0xBC, 0xD4), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Cyan
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            // Monitor shape: straight top and sides, curved bottom
            pathBuilder.MoveTo(r.Left, r.Top);
            pathBuilder.LineTo(r.Right, r.Top);
            var (right, toRight, toLeft, left) = BottomCurve(r);
            pathBuilder.LineTo(right);
            // Curved bottom
            pathBuilder.CubicTo(toRight, toLeft, left);
            
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
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
