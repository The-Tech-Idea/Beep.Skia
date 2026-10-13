using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Extract node: triangle for data extraction operations, pointing right (up when the flow runs top to bottom).
    /// Used for selecting, filtering, or extracting specific data from a dataset.
    /// </summary>
    public class ExtractNode : DirectedFlowchartControl
    {
        private string _label = "Extract";
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
        /// Extract operation description.
        /// </summary>
        public ExtractNode()
        {
            Name = "Flowchart Extract";
            Width = 120;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Extract operation description."
            };
        }

        /// <summary>The input on the left edge, the output at the point on the right; more of either spread along the left
        /// edge or the two slanting edges.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            PlaceStackBeside(InConnectionPoints, r.Left, -PortRadius, float.MaxValue);
            var count = OutConnectionPoints.Count;
            var step = count > 1 ? Math.Max(0, r.Height - 20f) / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
            {
                var y = r.MidY + (i - (count - 1) / 2f) * step;
                var x = r.Right - Math.Abs(y - r.MidY) / (r.Height / 2) * r.Width;
                SetPortBeside(OutConnectionPoints[i], new SKPoint(x, y), PortRadius, i);
            }
        }

        /// <summary>For a flow that runs top to bottom the triangle points up: inputs at the point (more spread over the two
        /// slanting edges either side of it), outputs along the flat bottom.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            PlaceAlongEdge(InConnectionPoints, r.MidX - r.Width * 0.2f, r.MidX + r.Width * 0.2f,
                x => r.Top + r.Height * Math.Abs(x - r.MidX) / (r.Width / 2), -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, _ => r.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;

            // Right-pointing triangle; for a flow that runs top to bottom, the usual up-pointing one
            var points = ShowTopBottomPorts
                ? new SKPoint[] { new(r.MidX, r.Top), new(r.Right, r.Bottom), new(r.Left, r.Bottom) }
                : new SKPoint[] { new(r.Left, r.Top), new(r.Right, r.MidY), new(r.Left, r.Bottom) };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE0, 0xF2, 0xF1), IsAntialias = true }; // Light teal
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x00, 0x96, 0x88), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Teal
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            pathBuilder.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                pathBuilder.LineTo(points[i]);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label
            float textX = ShowTopBottomPorts ? r.MidX - font.MeasureText(Label, text) / 2 : r.Left + 15;
            var ty = ShowTopBottomPorts ? r.Top + r.Height * 0.72f : r.MidY + 5;
            canvas.DrawText(Label, textX, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
