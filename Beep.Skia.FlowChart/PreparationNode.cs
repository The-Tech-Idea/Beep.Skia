using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Preparation node: horizontal hexagon for initialization/setup steps.
    /// Commonly used for variable declarations, configuration, or setup tasks.
    /// </summary>
    public class PreparationNode : DirectedFlowchartControl
    {
        private string _label = "Preparation";
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
        /// Text shown inside the preparation hexagon.
        /// </summary>
        public PreparationNode()
        {
            Name = "Flowchart Preparation";
            Width = 180;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the preparation hexagon."
            };
        }

        protected override void LayoutSidePorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        /// <summary>How far the top and bottom edges sit in from the pointed left and right ends.</summary>
        private static float Indent(SKRect b) => b.Width * 0.15f;

        /// <summary>Inputs along the flat top, outputs along the flat bottom, between the pointed ends.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var b = Bounds;
            var indent = Indent(b);
            PlaceAlongEdge(InConnectionPoints, b.Left + indent + 8f, b.Right - indent - 8f, _ => b.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, b.Left + indent + 8f, b.Right - indent - 8f, _ => b.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            float w = b.Width;
            float h = b.Height;
            var indent = Indent(b);

            // Horizontal hexagon points
            var points = new SKPoint[]
            {
                new SKPoint(b.Left + indent, b.Top),          // Top left
                new SKPoint(b.Right - indent, b.Top),         // Top right
                new SKPoint(b.Right, b.MidY),                 // Middle right
                new SKPoint(b.Right - indent, b.Bottom),      // Bottom right
                new SKPoint(b.Left + indent, b.Bottom),       // Bottom left
                new SKPoint(b.Left, b.MidY)                   // Middle left
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFD, 0xE0, 0xE0), IsAntialias = true }; // Light red
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xF4, 0x43, 0x36), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Red
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
            var tx = b.MidX - font.MeasureText(Label, text) / 2;
            var ty = b.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
