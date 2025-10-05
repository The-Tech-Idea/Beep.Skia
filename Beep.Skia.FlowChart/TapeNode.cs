using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Tape node: rounded bottom semicircle shape for magnetic tape storage.
    /// Legacy flowchart symbol for sequential tape-based storage.
    /// </summary>
    public class TapeNode : FlowchartControl
    {
        private string _label = "Tape";
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

        public TapeNode()
        {
            Name = "Flowchart Tape";
            Width = 140;
            Height = 90;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Tape storage label."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port top
            if (InConnectionPoints.Count > 0)
            {
                var inPt = InConnectionPoints[0];
                inPt.Center = new SKPoint(r.MidX, r.Top);
                inPt.Position = new SKPoint(r.MidX, r.Top - PortRadius);
                inPt.Bounds = new SKRect(
                    inPt.Center.X - PortRadius,
                    inPt.Center.Y - PortRadius,
                    inPt.Center.X + PortRadius,
                    inPt.Center.Y + PortRadius
                );
                inPt.Rect = inPt.Bounds;
            }

            // Output port bottom
            if (OutConnectionPoints.Count > 0)
            {
                var outPt = OutConnectionPoints[0];
                outPt.Center = new SKPoint(r.MidX, r.Bottom);
                outPt.Position = new SKPoint(r.MidX, r.Bottom + PortRadius);
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
            float waveHeight = r.Height * 0.15f;

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF1, 0xF8, 0xE9), IsAntialias = true }; // Light green
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x66, 0x9B, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Olive
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            // Wavy top edge
            path.MoveTo(r.Left, r.Top + waveHeight);
            path.CubicTo(
                r.Left + r.Width * 0.25f, r.Top,
                r.Left + r.Width * 0.75f, r.Top,
                r.Right, r.Top + waveHeight
            );

            // Right edge
            path.LineTo(r.Right, r.Bottom - waveHeight);

            // Wavy bottom edge
            path.CubicTo(
                r.Left + r.Width * 0.75f, r.Bottom,
                r.Left + r.Width * 0.25f, r.Bottom,
                r.Left, r.Bottom - waveHeight
            );

            // Left edge
            path.Close();

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
