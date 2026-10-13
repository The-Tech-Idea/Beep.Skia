using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Merge node: inverted triangle for combining multiple flow paths (no synchronization logic).
    /// </summary>
    public class MergeNode : DirectedFlowchartControl
    {
        private string _label = "Merge";
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
        /// Text shown inside the merge symbol.
        /// </summary>
        public MergeNode()
        {
            Name = "Flowchart Merge";
            Width = 100;
            Height = 80;
            EnsurePortCounts(2, 1); // Multiple inputs, single output

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the merge symbol."
            };
        }

        /// <summary>The inputs down the left edge, the output on the right; more outputs stack down the right.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            if (InConnectionPoints.Count > 0)
            {
                PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 10f, r.Bottom - 10f, outwardSign: -1f);
            }
            PlaceStackBeside(OutConnectionPoints, r.Right, PortRadius, float.MaxValue);
        }

        /// <summary>Inputs along the flat top, outputs at the point at the bottom (more spread over the two slanting edges
        /// either side of it).</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            PlaceAlongEdge(InConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.MidX - r.Width * 0.2f, r.MidX + r.Width * 0.2f,
                x => r.Bottom - r.Height * Math.Abs(x - r.MidX) / (r.Width / 2), +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            // Inverted triangle: top-left → top-right → bottom-center
            var topLeft = new SKPoint(b.Left, b.Top);
            var topRight = new SKPoint(b.Right, b.Top);
            var bottomCenter = new SKPoint(b.MidX, b.Bottom);

            using var fill = new SKPaint { Color = new SKColor(0xE1, 0xF5, 0xFE), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x03, 0xA9, 0xF4), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            pathBuilder.MoveTo(topLeft);
            pathBuilder.LineTo(topRight);
            pathBuilder.LineTo(bottomCenter);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label centered in upper portion of triangle
            var tx = b.MidX - font.MeasureText(Label, text) / 2;
            var ty = b.Top + b.Height * 0.35f;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
