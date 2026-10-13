using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Collate node: inverted trapezoid for sorting or collating data.
    /// Used to represent organizing or ordering operations.
    /// </summary>
    public class CollateNode : DirectedFlowchartControl
    {
        private string _label = "Collate";
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
        /// Text shown inside the collate shape.
        /// </summary>
        public CollateNode()
        {
            Name = "Flowchart Collate";
            Width = 120;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the collate shape."
            };
        }

        protected override void LayoutSidePorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        /// <summary>How far the top corners sit in from the bottom ones.</summary>
        private static float Slant(SKRect r) => r.Width * 0.2f;

        /// <summary>Inputs along the narrow top edge, outputs along the wide bottom edge.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var slant = Slant(r);
            PlaceAlongEdge(InConnectionPoints, r.Left + slant + 8f, r.Right - slant - 8f, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + 8f, r.Right - 8f, _ => r.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            var slant = Slant(r);

            // Inverted trapezoid: narrower at top, wider at bottom
            var points = new SKPoint[]
            {
                new SKPoint(r.Left + slant, r.Top),      // Top left
                new SKPoint(r.Right - slant, r.Top),     // Top right
                new SKPoint(r.Right, r.Bottom),          // Bottom right
                new SKPoint(r.Left, r.Bottom)            // Bottom left
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF3, 0xE5, 0xF5), IsAntialias = true }; // Light purple
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x9C, 0x27, 0xB0), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Purple
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

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
