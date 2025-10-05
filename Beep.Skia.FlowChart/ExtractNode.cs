using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Extract node: right-pointing triangle for data extraction operations.
    /// Used for selecting, filtering, or extracting specific data from a dataset.
    /// </summary>
    public class ExtractNode : FlowchartControl
    {
        private string _label = "Extract";
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

        public ExtractNode()
        {
            Name = "Flowchart Extract";
            Width = 120;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Extract operation description."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port on left
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

            // Output port on right
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

            // Right-pointing triangle
            var points = new SKPoint[]
            {
                new SKPoint(r.Left, r.Top),
                new SKPoint(r.Right, r.MidY),
                new SKPoint(r.Left, r.Bottom)
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE0, 0xF2, 0xF1), IsAntialias = true }; // Light teal
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x00, 0x96, 0x88), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Teal
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i]);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label
            float textX = r.Left + 15;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, textX, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
