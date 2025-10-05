using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// For loop node: rectangle with loop icon and condition text.
    /// Represents iterative processing with init, condition, and increment.
    /// </summary>
    public class ForLoopNode : FlowchartControl
    {
        private string _loopVariable = "i";
        public string LoopVariable
        {
            get => _loopVariable;
            set
            {
                var v = value ?? "i";
                if (!string.Equals(_loopVariable, v, System.StringComparison.Ordinal))
                {
                    _loopVariable = v;
                    if (NodeProperties.TryGetValue("LoopVariable", out var pi))
                        pi.ParameterCurrentValue = _loopVariable;
                    InvalidateVisual();
                }
            }
        }

        private string _initExpression = "i = 0";
        public string InitExpression
        {
            get => _initExpression;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_initExpression, v, System.StringComparison.Ordinal))
                {
                    _initExpression = v;
                    if (NodeProperties.TryGetValue("InitExpression", out var pi))
                        pi.ParameterCurrentValue = _initExpression;
                    InvalidateVisual();
                }
            }
        }

        private string _condition = "i < 10";
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

        private string _increment = "i++";
        public string Increment
        {
            get => _increment;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_increment, v, System.StringComparison.Ordinal))
                {
                    _increment = v;
                    if (NodeProperties.TryGetValue("Increment", out var pi))
                        pi.ParameterCurrentValue = _increment;
                    InvalidateVisual();
                }
            }
        }

        public ForLoopNode()
        {
            Name = "Flowchart For Loop";
            Width = 180;
            Height = 100;
            // Input (top), Body output (right), Exit output (bottom), Body return (left)
            EnsurePortCounts(2, 2);

            NodeProperties["LoopVariable"] = new ParameterInfo
            {
                ParameterName = "LoopVariable",
                ParameterType = typeof(string),
                DefaultParameterValue = _loopVariable,
                ParameterCurrentValue = _loopVariable,
                Description = "Loop variable name (e.g., 'i', 'item')."
            };
            NodeProperties["InitExpression"] = new ParameterInfo
            {
                ParameterName = "InitExpression",
                ParameterType = typeof(string),
                DefaultParameterValue = _initExpression,
                ParameterCurrentValue = _initExpression,
                Description = "Initialization expression (e.g., 'i = 0')."
            };
            NodeProperties["Condition"] = new ParameterInfo
            {
                ParameterName = "Condition",
                ParameterType = typeof(string),
                DefaultParameterValue = _condition,
                ParameterCurrentValue = _condition,
                Description = "Loop condition (e.g., 'i < 10')."
            };
            NodeProperties["Increment"] = new ParameterInfo
            {
                ParameterName = "Increment",
                ParameterType = typeof(string),
                DefaultParameterValue = _increment,
                ParameterCurrentValue = _increment,
                Description = "Increment expression (e.g., 'i++', 'i += 2')."
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

            // Body return port: left side (return from body to next iteration)
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

            // Body output port: right side (execute loop body)
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

            // Exit port: bottom center (after loop completes)
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
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xF3, 0xE5, 0xF5), IsAntialias = true }; // Light purple
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x9C, 0x27, 0xB0), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Purple
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 11);
            using var loopIcon = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x9C, 0x27, 0xB0), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            // Draw rounded rectangle
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Draw loop icon (circular arrow) in upper left
            float iconSize = 18f;
            float iconX = r.Left + 10;
            float iconY = r.Top + 10;
            using var iconPath = new SKPath();
            iconPath.AddArc(new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize), 45, 270);
            // Add arrow head
            iconPath.MoveTo(iconX + iconSize * 0.85f, iconY);
            iconPath.LineTo(iconX + iconSize * 0.85f, iconY + 5);
            iconPath.LineTo(iconX + iconSize + 2, iconY + 2.5f);
            iconPath.Close();
            canvas.DrawPath(iconPath, loopIcon);

            // Draw loop text
            float textY = r.Top + 32;
            float textX = r.Left + 8;
            
            // for (init; condition; increment)
            string loopText = $"for ({InitExpression}; {Condition}; {Increment})";
            
            // Word wrap if too long
            if (font.MeasureText(loopText, text) > r.Width - 16)
            {
                // Split into multiple lines
                canvas.DrawText($"for ({InitExpression};", textX, textY, SKTextAlign.Left, font, text);
                textY += 14;
                canvas.DrawText($"  {Condition};", textX, textY, SKTextAlign.Left, font, text);
                textY += 14;
                canvas.DrawText($"  {Increment})", textX, textY, SKTextAlign.Left, font, text);
            }
            else
            {
                canvas.DrawText(loopText, textX, textY, SKTextAlign.Left, font, text);
            }

            DrawPorts(canvas);
        }
    }
}
