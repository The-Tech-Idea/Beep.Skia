using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Summing Junction node: X in a circle for combining/merging multiple inputs.
    /// Different from OR gate - represents mathematical summation or aggregation.
    /// </summary>
    public class SummingJunctionNode : FlowchartControl
    {
        private string _label = "";
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

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Multiple input ports on left
            if (InConnectionPoints.Count >= 2)
            {
                float spacing = r.Height * 0.5f;
                float startY = r.MidY - spacing / 2;

                for (int i = 0; i < InConnectionPoints.Count && i < 2; i++)
                {
                    var pt = InConnectionPoints[i];
                    float y = startY + (i * spacing);
                    pt.Center = new SKPoint(r.Left, y);
                    pt.Position = new SKPoint(r.Left - PortRadius, y);
                    pt.Bounds = new SKRect(
                        pt.Center.X - PortRadius,
                        pt.Center.Y - PortRadius,
                        pt.Center.X + PortRadius,
                        pt.Center.Y + PortRadius
                    );
                    pt.Rect = pt.Bounds;
                }
            }

            // One output port on right
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
                using var labelFont = new SKFont(SKTypeface.Default, 10);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                float labelWidth = labelFont.MeasureText(Label, grayText);
                canvas.DrawText(Label, center.X - labelWidth / 2, r.Bottom + 12, SKTextAlign.Left, labelFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
