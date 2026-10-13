using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Manual operation node: trapezoid shape (top wider than bottom) for human-performed tasks.
    /// </summary>
    public class ManualOperationNode : DirectedFlowchartControl
    {
        private string _label = "Manual Operation";
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
        /// Text shown inside the trapezoid.
        /// </summary>
        public ManualOperationNode()
        {
            Name = "Flowchart Manual Operation";
            Width = 160;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the trapezoid."
            };
        }

        protected override void LayoutSidePorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        /// <summary>How far the bottom corners sit in from the top ones.</summary>
        private static float Slant(SKRect r) => r.Width * 0.15f;

        /// <summary>Inputs along the wide top edge, outputs along the narrow bottom edge.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var slant = Slant(r);
            PlaceAlongEdge(InConnectionPoints, r.Left + 8f, r.Right - 8f, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + slant + 8f, r.Right - slant - 8f, _ => r.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            var slant = Slant(r); // inward at the bottom

            // Trapezoid: top-left → top-right → bottom-right-inner → bottom-left-inner
            var topLeft = new SKPoint(r.Left, r.Top);
            var topRight = new SKPoint(r.Right, r.Top);
            var bottomRight = new SKPoint(r.Right - slant, r.Bottom);
            var bottomLeft = new SKPoint(r.Left + slant, r.Bottom);

            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xEB, 0xEE), IsAntialias = true }; // Light pink
            using var stroke = new SKPaint { Color = new SKColor(0xE5, 0x39, 0x35), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Red
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            pathBuilder.MoveTo(topLeft);
            pathBuilder.LineTo(topRight);
            pathBuilder.LineTo(bottomRight);
            pathBuilder.LineTo(bottomLeft);
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
