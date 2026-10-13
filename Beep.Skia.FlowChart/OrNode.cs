using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Or node: logic gate shape for conditional OR operations. Circle with OR symbol.
    /// Used when any input condition can proceed to the next step.
    /// </summary>
    public class OrNode : DirectedFlowchartControl
    {
        private string _label = "OR";
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
        /// Label for the OR gate (optional).
        /// </summary>
        public OrNode()
        {
            Name = "Flowchart OR Gate";
            Width = 80;
            Height = 80;
            EnsurePortCounts(2, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Label for the OR gate (optional)."
            };
        }

        /// <summary>Inputs stacked down the left, the output on the right.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            PlaceStackBeside(InConnectionPoints, r.Left, -PortRadius, r.Height * 0.4f);
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

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFC, 0xE4, 0xEC), IsAntialias = true }; // Light pink
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xE9, 0x1E, 0x63), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Pink
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 16);

            // Draw circle
            canvas.DrawCircle(center, radius, fill);
            canvas.DrawCircle(center, radius, stroke);

            // Draw OR symbol (≥1)
            using var symbolFont = new SKFont(Beep.Skia.TypefaceCache.Default, 18) { Embolden = true };
            using var boldText = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            string orSymbol = "≥1";
            float symbolWidth = symbolFont.MeasureText(orSymbol, boldText);
            canvas.DrawText(orSymbol, center.X - symbolWidth / 2, center.Y + 6, SKTextAlign.Left, symbolFont, boldText);

            // Draw label below if provided
            if (!string.IsNullOrWhiteSpace(Label) && Label != "OR")
            {
                using var labelFont = new SKFont(Beep.Skia.TypefaceCache.Default, 10);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                float labelWidth = labelFont.MeasureText(Label, grayText);
                canvas.DrawText(Label, center.X - labelWidth / 2, r.Bottom - 5, SKTextAlign.Left, labelFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
