using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Delay node: half-circle or rounded shape indicating a pause/wait in the process.
    /// </summary>
    public class DelayNode : DirectedFlowchartControl
    {
        private string _label = "Delay";
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

        private string _duration = "";
        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public string Duration
        {
            get => _duration;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_duration, v, System.StringComparison.Ordinal))
                {
                    _duration = v;
                    if (NodeProperties.TryGetValue("Duration", out var pi))
                        pi.ParameterCurrentValue = _duration;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Delay description.
        /// </summary>
        public DelayNode()
        {
            Name = "Flowchart Delay";
            Width = 140;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Delay description."
            };
            NodeProperties["Duration"] = new ParameterInfo
            {
                ParameterName = "Duration",
                ParameterType = typeof(string),
                DefaultParameterValue = _duration,
                ParameterCurrentValue = _duration,
                Description = "Delay duration (e.g., '5 sec', '10 min', '1 hour')."
            };
        }

        /// <summary>The input on the straight left edge, the output on the curve at the right; more of either spread along them.</summary>
        protected override void LayoutSidePorts()
        {
            var r = Bounds;
            PlaceStackBeside(InConnectionPoints, r.Left, -PortRadius, float.MaxValue);
            var count = OutConnectionPoints.Count;
            for (int i = 0; i < count; i++)
            {
                // One output sits at the tip of the curve; more spread over its middle 100 degrees.
                var angle = count == 1 ? 0f : (-50f + 100f * i / (count - 1)) * MathF.PI / 180f;
                var at = new SKPoint(r.MidX + r.Width / 2 * MathF.Cos(angle), r.MidY + r.Height / 2 * MathF.Sin(angle));
                SetPortBeside(OutConnectionPoints[i], at, PortRadius, i);
            }
        }

        /// <summary>Inputs and outputs along the flat top and bottom, which run from the left edge to the middle.</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            PlaceAlongEdge(InConnectionPoints, r.Left + 8f, r.MidX - 8f, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, r.Left + 8f, r.MidX - 8f, _ => r.Bottom, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFF, 0xF8, 0xE1), IsAntialias = true }; // Light amber
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xFF, 0xA0, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Orange
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            // D-shape: straight left edge, flat top and bottom to the middle, then the right half of the ellipse in the bounds
            pathBuilder.MoveTo(r.Left, r.Top);
            pathBuilder.LineTo(r.MidX, r.Top);
            pathBuilder.ArcTo(new SKRect(r.Left, r.Top, r.Right, r.Bottom), -90, 180, false);
            pathBuilder.LineTo(r.Left, r.Bottom);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label
            float textY = r.MidY;
            if (!string.IsNullOrWhiteSpace(Duration))
            {
                textY = r.MidY - 6;
            }

            var tx = r.Left + 15;
            canvas.DrawText(Label, tx, textY, SKTextAlign.Left, font, text);

            // Draw duration if provided
            if (!string.IsNullOrWhiteSpace(Duration))
            {
                using var smallFont = new SKFont(Beep.Skia.TypefaceCache.Default, 11);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                canvas.DrawText(Duration, tx, r.MidY + 10, SKTextAlign.Left, smallFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
