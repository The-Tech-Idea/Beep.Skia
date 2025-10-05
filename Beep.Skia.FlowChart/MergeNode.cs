using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Merge node: inverted triangle for combining multiple flow paths (no synchronization logic).
    /// </summary>
    public class MergeNode : FlowchartControl
    {
        private string _label = "Merge";
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

        public MergeNode()
        {
            Name = "Flowchart Merge";
            Width = 100;
            Height = 80;
            EnsurePortCounts(2, 1); // Multiple inputs, single output

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the merge symbol."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;
            
            // Input ports on left side (vertical distribution)
            if (InConnectionPoints.Count > 0)
            {
                PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 10f, r.Bottom - 10f, outwardSign: -1f);
            }

            // Single output port on right side (center)
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

            var b = Bounds;
            // Inverted triangle: top-left → top-right → bottom-center
            var topLeft = new SKPoint(b.Left, b.Top);
            var topRight = new SKPoint(b.Right, b.Top);
            var bottomCenter = new SKPoint(b.MidX, b.Bottom);

            using var fill = new SKPaint { Color = new SKColor(0xE1, 0xF5, 0xFE), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x03, 0xA9, 0xF4), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(topLeft);
            path.LineTo(topRight);
            path.LineTo(bottomCenter);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label centered in upper portion of triangle
            var tx = b.MidX - font.MeasureText(Label, text) / 2;
            var ty = b.Top + b.Height * 0.35f;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
