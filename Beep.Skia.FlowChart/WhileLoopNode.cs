using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// While loop node: hexagon (vertical orientation) with condition text.
    /// Represents loop that executes while condition is true.
    /// </summary>
    public class WhileLoopNode : FlowchartControl
    {
        private string _condition = "condition";
        public string Condition
        {
            get => _condition;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_condition, v, System.StringComparison.Ordinal))
                {
                    _condition = v;
                    if (NodeProperties.TryGetValue("Condition", out var pi))
                        pi.ParameterCurrentValue = _condition;
                    InvalidateVisual();
                }
            }
        }

        public WhileLoopNode()
        {
            Name = "Flowchart While Loop";
            Width = 140;
            Height = 100;
            // Input (top), Body output (right), Exit output (bottom), Body return (left)
            EnsurePortCounts(2, 2);

            NodeProperties["Condition"] = new ParameterInfo
            {
                ParameterName = "Condition",
                ParameterType = typeof(string),
                DefaultParameterValue = _condition,
                ParameterCurrentValue = _condition,
                Description = "Loop condition expression (e.g., 'count > 0', 'hasMore')."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port: top center (entry to loop)
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

            // Body return port: left side (return from body to check condition)
            if (InConnectionPoints.Count > 1)
            {
                var returnPt = InConnectionPoints[1];
                returnPt.Center = new SKPoint(r.Left, r.MidY);
                returnPt.Position = new SKPoint(r.Left - PortRadius, r.MidY);
                returnPt.Bounds = new SKRect(
                    returnPt.Center.X - PortRadius,
                    returnPt.Center.Y - PortRadius,
                    returnPt.Center.X + PortRadius,
                    returnPt.Center.Y + PortRadius
                );
                returnPt.Rect = returnPt.Bounds;
            }

            // Body output port: right side (execute loop body when condition true)
            if (OutConnectionPoints.Count > 0)
            {
                var bodyPt = OutConnectionPoints[0];
                bodyPt.Center = new SKPoint(r.Right, r.MidY);
                bodyPt.Position = new SKPoint(r.Right + PortRadius, r.MidY);
                bodyPt.Bounds = new SKRect(
                    bodyPt.Center.X - PortRadius,
                    bodyPt.Center.Y - PortRadius,
                    bodyPt.Center.X + PortRadius,
                    bodyPt.Center.Y + PortRadius
                );
                bodyPt.Rect = bodyPt.Bounds;
            }

            // Exit port: bottom center (when condition false)
            if (OutConnectionPoints.Count > 1)
            {
                var exitPt = OutConnectionPoints[1];
                exitPt.Center = new SKPoint(r.MidX, r.Bottom);
                exitPt.Position = new SKPoint(r.MidX, r.Bottom + PortRadius);
                exitPt.Bounds = new SKRect(
                    exitPt.Center.X - PortRadius,
                    exitPt.Center.Y - PortRadius,
                    exitPt.Center.X + PortRadius,
                    exitPt.Center.Y + PortRadius
                );
                exitPt.Rect = exitPt.Bounds;
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            float w = b.Width;
            float h = b.Height;
            float indent = w * 0.25f;

            // Vertical hexagon points
            var points = new SKPoint[]
            {
                new SKPoint(b.MidX, b.Top),                      // Top center
                new SKPoint(b.Right, b.Top + indent),            // Top right
                new SKPoint(b.Right, b.Bottom - indent),         // Bottom right
                new SKPoint(b.MidX, b.Bottom),                   // Bottom center
                new SKPoint(b.Left, b.Bottom - indent),          // Bottom left
                new SKPoint(b.Left, b.Top + indent)              // Top left
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE8, 0xEA, 0xF6), IsAntialias = true }; // Light indigo
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x3F, 0x51, 0xB5), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Indigo
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 12);
            using var path = new SKPath();

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i]);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw "while" label
            using var labelFont = new SKFont(SKTypeface.Default, 10);
            canvas.DrawText("while", b.Left + 8, b.Top + indent + 12, SKTextAlign.Left, labelFont, text);

            // Draw condition centered
            float condWidth = font.MeasureText(Condition, text);
            float tx = b.MidX - condWidth / 2;
            float ty = b.MidY + 5;
            canvas.DrawText(Condition, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
