using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Delay node: half-circle or rounded shape indicating a pause/wait in the process.
    /// </summary>
    public class DelayNode : FlowchartControl
    {
        private string _label = "Delay";
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

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port: left side
            if (InConnectionPoints.Count > 0)
            {
                var inPt = InConnectionPoints[0];
                inPt.Center = new SKPoint(r.Left, r.MidY);
                inPt.Position = new SKPoint(r.Left - PortRadius, r.MidY);
                inPt.Bounds = new SKRect(
                    inPt.Center.X - PortRadius,
                    inPt.Center.Y - PortRadius,
                    inPt.Center.X + PortRadius,
                    inPt.Center.Y + PortRadius
                );
                inPt.Rect = inPt.Bounds;
            }

            // Output port: right side
            if (OutConnectionPoints.Count > 0)
            {
                var outPt = OutConnectionPoints[0];
                outPt.Center = new SKPoint(r.Right, r.MidY);
                outPt.Position = new SKPoint(r.Right + PortRadius, r.MidY);
                outPt.Bounds = new SKRect(
                    outPt.Center.X - PortRadius,
                    outPt.Center.Y - PortRadius,
                    outPt.Center.X + PortRadius,
                    outPt.Center.Y + PortRadius
                );
                outPt.Rect = outPt.Bounds;
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFF, 0xF8, 0xE1), IsAntialias = true }; // Light amber
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xFF, 0xA0, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Orange
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            // D-shape: straight left edge, curved right edge
            path.MoveTo(r.Left, r.Top);
            path.LineTo(r.Left, r.Bottom);
            
            // Arc on the right side (half circle)
            path.ArcTo(
                new SKRect(r.Left, r.Top, r.Right, r.Bottom),
                90,  // Start angle
                180, // Sweep angle
                false
            );
            
            path.Close();

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
                using var smallFont = new SKFont(SKTypeface.Default, 11);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                canvas.DrawText(Duration, tx, r.MidY + 10, SKTextAlign.Left, smallFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
