using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Stored data node: parallelogram tilted to the right for data storage (tape, disk).
    /// Different from InputOutput which tilts both sides.
    /// </summary>
    public class StoredDataNode : DirectedFlowchartControl
    {
        private string _label = "Stored Data";
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
        /// Text shown inside the stored data symbol.
        /// </summary>
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

        protected override void LayoutSidePorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        /// <summary>How far the curved left end leans.</summary>
        private static float Slant(SKRect r) => r.Height * 0.2f;

        /// <summary>Inputs along the flat top, outputs along the flat bottom, from just past the curved left end.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var from = r.Left + Slant(r) * 0.3f + FlowInset;
            PlaceAlongEdge(InConnectionPoints, from, r.Right - FlowInset, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, from, r.Right - FlowInset, _ => r.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            var slant = Slant(r);

            // Parallelogram tilted right (curved left edge for tape/disk appearance)
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF1, 0xF8, 0xE9), IsAntialias = true }; // Light green
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x66, 0x9B, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Olive green
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            // Left edge is curved (like tape reel)
            pathBuilder.MoveTo(r.Left + slant * 0.3f, r.Top);
            pathBuilder.CubicTo(
                r.Left - slant * 0.2f, r.Top + r.Height * 0.3f,
                r.Left - slant * 0.2f, r.Top + r.Height * 0.7f,
                r.Left + slant * 0.3f, r.Bottom
            );
            
            // Bottom edge
            pathBuilder.LineTo(r.Right, r.Bottom);
            
            // Right edge (straight)
            pathBuilder.LineTo(r.Right, r.Top);
            
            // Top edge
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
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
