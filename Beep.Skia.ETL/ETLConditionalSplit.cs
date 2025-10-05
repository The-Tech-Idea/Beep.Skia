using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Conditional Split component that routes rows to multiple outputs based on conditions.
    /// Displays as a diamond with multiple output ports.
    /// Evaluates conditions in order; first match wins. Unmatched rows go to default output.
    /// </summary>
    public class ETLConditionalSplit : ETLControl
    {
        private string _conditionsJson = "[]";
        public string Conditions
        {
            get => _conditionsJson;
            set
            {
                var v = value ?? "[]";
                if (_conditionsJson == v) return;
                _conditionsJson = v;
                if (NodeProperties.TryGetValue("Conditions", out var p))
                    p.ParameterCurrentValue = _conditionsJson;
                else
                    NodeProperties["Conditions"] = new ParameterInfo
                    {
                        ParameterName = "Conditions",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _conditionsJson,
                        ParameterCurrentValue = _conditionsJson,
                        Description = "Split conditions (JSON array of SplitCondition)"
                    };
                // Update output port count based on conditions
                UpdateOutputPorts();
                InvalidateVisual();
            }
        }

        private bool _hasDefaultOutput = true;
        public bool HasDefaultOutput
        {
            get => _hasDefaultOutput;
            set
            {
                if (_hasDefaultOutput == value) return;
                _hasDefaultOutput = value;
                if (NodeProperties.TryGetValue("HasDefaultOutput", out var p))
                    p.ParameterCurrentValue = _hasDefaultOutput;
                else
                    NodeProperties["HasDefaultOutput"] = new ParameterInfo
                    {
                        ParameterName = "HasDefaultOutput",
                        ParameterType = typeof(bool),
                        DefaultParameterValue = _hasDefaultOutput,
                        ParameterCurrentValue = _hasDefaultOutput,
                        Description = "Include default output for unmatched rows"
                    };
                UpdateOutputPorts();
                InvalidateVisual();
            }
        }

        public ETLConditionalSplit()
        {
            Title = "Conditional Split";
            HeaderColor = MaterialControl.MaterialColors.SecondaryContainer;
            Width = 180;
            Height = 110;
            DisplayText = "Split";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            // 1 input, multiple outputs (start with 2: 1 condition + 1 default)
            EnsurePortCounts(inCount: 1, outCount: 2);

            NodeProperties["Conditions"] = new ParameterInfo
            {
                ParameterName = "Conditions",
                ParameterType = typeof(string),
                DefaultParameterValue = _conditionsJson,
                ParameterCurrentValue = _conditionsJson,
                Description = "Split conditions (JSON array of SplitCondition)"
            };
            NodeProperties["HasDefaultOutput"] = new ParameterInfo
            {
                ParameterName = "HasDefaultOutput",
                ParameterType = typeof(bool),
                DefaultParameterValue = _hasDefaultOutput,
                ParameterCurrentValue = _hasDefaultOutput,
                Description = "Include default output for unmatched rows"
            };
        }

        private void UpdateOutputPorts()
        {
            try
            {
                var conditions = System.Text.Json.JsonSerializer.Deserialize<List<SplitCondition>>(_conditionsJson) ?? new();
                int conditionCount = conditions.Count;
                int totalOutputs = conditionCount + (_hasDefaultOutput ? 1 : 0);
                if (totalOutputs < 1) totalOutputs = 1; // At least one output
                
                if (OutConnectionPoints.Count != totalOutputs)
                {
                    EnsurePortCounts(inCount: 1, outCount: totalOutputs);
                }
            }
            catch { }
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            // Create diamond
            path.MoveTo(rect.MidX, rect.Top);
            path.LineTo(rect.Right, rect.MidY);
            path.LineTo(rect.MidX, rect.Bottom);
            path.LineTo(rect.Left, rect.MidY);
            path.Close();

            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawPath(path, fill);

            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            canvas.DrawPath(path, border);

            // Draw vertical line in middle to indicate split
            using var splitLine = new SKPaint
            {
                Color = Stroke.WithAlpha((byte)(Stroke.Alpha * 0.5f)),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 4, 2 }, 0)
            };
            canvas.DrawLine(rect.MidX, rect.Top + 20, rect.MidX, rect.Bottom - 20, splitLine);
        }

        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Input at top point of diamond
            if (InConnectionPoints.Count > 0)
            {
                var inputPoint = InConnectionPoints[0];
                inputPoint.Center = new SKPoint(rect.MidX, rect.Top - PortRadius - 2);
                inputPoint.Position = inputPoint.Center;
                float r = PortRadius;
                inputPoint.Bounds = new SKRect(inputPoint.Center.X - r, inputPoint.Center.Y - r,
                                             inputPoint.Center.X + r, inputPoint.Center.Y + r);
                inputPoint.Rect = inputPoint.Bounds;
                inputPoint.Index = 0;
                inputPoint.Component = this;
                inputPoint.IsAvailable = true;
            }

            // Multiple outputs along right and bottom edges
            int outputCount = OutConnectionPoints.Count;
            for (int i = 0; i < outputCount; i++)
            {
                var outputPoint = OutConnectionPoints[i];
                float t = outputCount > 1 ? i / (float)(outputCount - 1) : 0.5f;
                
                // Distribute outputs: right side (45-135 degrees) and bottom point (135 degrees)
                SKPoint center;
                if (outputCount == 1)
                {
                    center = new SKPoint(rect.Right + PortRadius + 2, rect.MidY);
                }
                else if (i < outputCount / 2)
                {
                    // Right side
                    float rightT = (i + 1) / (float)(outputCount / 2 + 1);
                    center = new SKPoint(rect.Right + PortRadius + 2, rect.Top + Height * rightT);
                }
                else
                {
                    // Bottom point
                    center = new SKPoint(rect.MidX, rect.Bottom + PortRadius + 2);
                }

                outputPoint.Center = center;
                outputPoint.Position = outputPoint.Center;
                float r = PortRadius;
                outputPoint.Bounds = new SKRect(outputPoint.Center.X - r, outputPoint.Center.Y - r,
                                              outputPoint.Center.X + r, outputPoint.Center.Y + r);
                outputPoint.Rect = outputPoint.Bounds;
                outputPoint.Index = i;
                outputPoint.Component = this;
                outputPoint.IsAvailable = true;
            }
        }
    }
}
