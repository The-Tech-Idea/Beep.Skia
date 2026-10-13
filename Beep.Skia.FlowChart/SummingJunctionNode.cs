using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Summing Junction node: X in a circle for combining/merging multiple inputs.
    /// Different from OR gate - represents mathematical summation or aggregation.
    /// </summary>
    public class SummingJunctionNode : DirectedFlowchartControl
    {
        private string _label = "";
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
        /// Optional label for the summing junction.
        /// </summary>
        public SummingJunctionNode()
        {
            Name = "Flowchart Summing Junction";
            Width = 70;
            Height = 70;
            EnsurePortCounts(2, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Optional label for the summing junction."
            };
        }

        /// <summary>Inputs stacked down the left, the output on the right.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            PlaceStackBeside(InConnectionPoints, r.Left, -PortRadius, r.Height * 0.5f);
            PlaceStackBeside(OutConnectionPoints, r.Right, PortRadius, float.MaxValue);
        }

        /// <summary>Inputs spread over the top of the circle, outputs over the bottom, left to right.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var center = new SKPoint(r.MidX, r.MidY);
            var radius = Math.Min(r.Width, r.Height) / 2;
            const float degrees = MathF.PI / 180f;
            PlacePortsOnArc(InConnectionPoints, center, radius, -150f * degrees, -30f * degrees, outwardSign: +1f);
            PlacePortsOnArc(OutConnectionPoints, center, radius, 150f * degrees, 30f * degrees, outwardSign: +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float radius = Math.Min(r.Width, r.Height) / 2;
            var center = new SKPoint(r.MidX, r.MidY);

            using var fill = new SKPaint { Color = CustomFillColor ?? SKColors.White, IsAntialias = true };
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x42, 0x42, 0x42), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Dark gray
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };

            // Draw circle
            canvas.DrawCircle(center, radius, fill);
            canvas.DrawCircle(center, radius, stroke);

            // Draw X symbol inside
            float xSize = radius * 0.6f;
            using var xStroke = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            
            // Diagonal lines forming X
            canvas.DrawLine(
                center.X - xSize, center.Y - xSize,
                center.X + xSize, center.Y + xSize,
                xStroke
            );
            canvas.DrawLine(
                center.X - xSize, center.Y + xSize,
                center.X + xSize, center.Y - xSize,
                xStroke
            );

            // Draw label below if provided
            if (!string.IsNullOrWhiteSpace(Label))
            {
                using var labelFont = new SKFont(Beep.Skia.TypefaceCache.Default, 10);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                float labelWidth = labelFont.MeasureText(Label, grayText);
                canvas.DrawText(Label, center.X - labelWidth / 2, r.Bottom + 12, SKTextAlign.Left, labelFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
