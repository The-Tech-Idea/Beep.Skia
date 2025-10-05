using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Do-While loop node: executes body at least once, then checks condition at end.
    /// Similar to WhileLoop but with condition checked after execution.
    /// </summary>
    public class DoWhileLoopNode : FlowchartControl
    {
        private string _condition = "condition";
        public string Condition
        {
            get => _condition;
            set
            {
                var v = value ?? "condition";
                if (!string.Equals(_condition, v, System.StringComparison.Ordinal))
                {
                    _condition = v;
                    if (NodeProperties.TryGetValue("Condition", out var pi))
                        pi.ParameterCurrentValue = _condition;
                    InvalidateVisual();
                }
            }
        }

        public DoWhileLoopNode()
        {
            Name = "Flowchart Do-While Loop";
            Width = 160;
            Height = 100;
            EnsurePortCounts(2, 2);

            NodeProperties["Condition"] = new ParameterInfo
            {
                ParameterName = "Condition",
                ParameterType = typeof(string),
                DefaultParameterValue = _condition,
                ParameterCurrentValue = _condition,
                Description = "Boolean condition evaluated after each loop iteration."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Entry port: top center
            if (InConnectionPoints.Count > 0)
            {
                var entryPt = InConnectionPoints[0];
                entryPt.Center = new SKPoint(r.MidX, r.Top);
                entryPt.Position = new SKPoint(r.MidX, r.Top - PortRadius);
                entryPt.Bounds = new SKRect(
                    entryPt.Center.X - PortRadius,
                    entryPt.Center.Y - PortRadius,
                    entryPt.Center.X + PortRadius,
                    entryPt.Center.Y + PortRadius
                );
                entryPt.Rect = entryPt.Bounds;
            }

            // Loop back port: bottom left (return from body)
            if (InConnectionPoints.Count > 1)
            {
                var loopBackPt = InConnectionPoints[1];
                loopBackPt.Center = new SKPoint(r.Left, r.Bottom - r.Height * 0.2f);
                loopBackPt.Position = new SKPoint(r.Left - PortRadius, loopBackPt.Center.Y);
                loopBackPt.Bounds = new SKRect(
                    loopBackPt.Center.X - PortRadius,
                    loopBackPt.Center.Y - PortRadius,
                    loopBackPt.Center.X + PortRadius,
                    loopBackPt.Center.Y + PortRadius
                );
                loopBackPt.Rect = loopBackPt.Bounds;
            }

            // Body output: right side
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

            // Exit port: bottom center (condition false)
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

            var r = Bounds;
            float indent = r.Width * 0.25f;

            // Vertical hexagon (same as WhileLoop)
            var points = new SKPoint[]
            {
                new SKPoint(r.MidX, r.Top),
                new SKPoint(r.Right, r.Top + indent),
                new SKPoint(r.Right, r.Bottom - indent),
                new SKPoint(r.MidX, r.Bottom),
                new SKPoint(r.Left, r.Bottom - indent),
                new SKPoint(r.Left, r.Top + indent)
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE8, 0xEA, 0xF6), IsAntialias = true }; // Light indigo
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x3F, 0x51, 0xB5), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Indigo
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i]);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw "do-while" label at top
            using var labelFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var boldText = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            float labelWidth = labelFont.MeasureText("do-while", boldText);
            canvas.DrawText("do-while", r.MidX - labelWidth / 2, r.Top + 20, SKTextAlign.Left, labelFont, boldText);

            // Draw condition at bottom (where it's evaluated)
            using var condFont = new SKFont(SKTypeface.Default, 12);
            float condY = r.Bottom - 15;
            
            // Word wrap condition if too long
            float maxWidth = r.Width - 20;
            float condWidth = condFont.MeasureText(Condition, text);
            
            if (condWidth <= maxWidth)
            {
                float condX = r.MidX - condWidth / 2;
                canvas.DrawText(Condition, condX, condY, SKTextAlign.Left, condFont, text);
            }
            else
            {
                // Simple word wrap
                var words = Condition.Split(' ');
                string line = "";
                float lineY = condY - 10;
                
                foreach (var word in words)
                {
                    string testLine = string.IsNullOrEmpty(line) ? word : line + " " + word;
                    float testWidth = condFont.MeasureText(testLine, text);
                    
                    if (testWidth > maxWidth && !string.IsNullOrEmpty(line))
                    {
                        float lineX = r.MidX - condFont.MeasureText(line, text) / 2;
                        canvas.DrawText(line, lineX, lineY, SKTextAlign.Left, condFont, text);
                        line = word;
                        lineY += 15;
                    }
                    else
                    {
                        line = testLine;
                    }
                }
                
                if (!string.IsNullOrEmpty(line))
                {
                    float lineX = r.MidX - condFont.MeasureText(line, text) / 2;
                    canvas.DrawText(line, lineX, lineY, SKTextAlign.Left, condFont, text);
                }
            }

            DrawPorts(canvas);
        }
    }
}
