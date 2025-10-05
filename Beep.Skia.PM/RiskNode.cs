using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Risk node: tracks project risks with probability and impact assessments.
    /// Warning triangle shape with risk level color coding.
    /// </summary>
    public class RiskNode : PMControl
    {
        private string _riskDescription = "Risk";
        public string RiskDescription
        {
            get => _riskDescription;
            set
            {
                var v = value ?? "";
                if (_riskDescription != v)
                {
                    _riskDescription = v;
                    if (NodeProperties.TryGetValue("RiskDescription", out var p))
                        p.ParameterCurrentValue = _riskDescription;
                    InvalidateVisual();
                }
            }
        }

        private string _probability = "Medium";
        public string Probability
        {
            get => _probability;
            set
            {
                var v = value ?? "Medium";
                if (_probability != v)
                {
                    _probability = v;
                    if (NodeProperties.TryGetValue("Probability", out var p))
                        p.ParameterCurrentValue = _probability;
                    InvalidateVisual();
                }
            }
        }

        private string _impact = "Medium";
        public string Impact
        {
            get => _impact;
            set
            {
                var v = value ?? "Medium";
                if (_impact != v)
                {
                    _impact = v;
                    if (NodeProperties.TryGetValue("Impact", out var p))
                        p.ParameterCurrentValue = _impact;
                    InvalidateVisual();
                }
            }
        }

        private string _mitigationPlan = "";
        public string MitigationPlan
        {
            get => _mitigationPlan;
            set
            {
                var v = value ?? "";
                if (_mitigationPlan != v)
                {
                    _mitigationPlan = v;
                    if (NodeProperties.TryGetValue("MitigationPlan", out var p))
                        p.ParameterCurrentValue = _mitigationPlan;
                    InvalidateVisual();
                }
            }
        }

        public RiskNode()
        {
            Name = "PM Risk";
            Width = 150;
            Height = 100;
            EnsurePortCounts(1, 1);

            NodeProperties["RiskDescription"] = new ParameterInfo
            {
                ParameterName = "RiskDescription",
                ParameterType = typeof(string),
                DefaultParameterValue = _riskDescription,
                ParameterCurrentValue = _riskDescription,
                Description = "Risk description"
            };
            NodeProperties["Probability"] = new ParameterInfo
            {
                ParameterName = "Probability",
                ParameterType = typeof(string),
                DefaultParameterValue = _probability,
                ParameterCurrentValue = _probability,
                Description = "Probability of occurrence",
                Choices = new[] { "Low", "Medium", "High", "Very High" }
            };
            NodeProperties["Impact"] = new ParameterInfo
            {
                ParameterName = "Impact",
                ParameterType = typeof(string),
                DefaultParameterValue = _impact,
                ParameterCurrentValue = _impact,
                Description = "Impact if occurs",
                Choices = new[] { "Low", "Medium", "High", "Critical" }
            };
            NodeProperties["MitigationPlan"] = new ParameterInfo
            {
                ParameterName = "MitigationPlan",
                ParameterType = typeof(string),
                DefaultParameterValue = _mitigationPlan,
                ParameterCurrentValue = _mitigationPlan,
                Description = "Risk mitigation strategy"
            };
        }

        private int GetRiskLevel()
        {
            int probScore = Probability switch { "Very High" => 4, "High" => 3, "Medium" => 2, _ => 1 };
            int impactScore = Impact switch { "Critical" => 4, "High" => 3, "Medium" => 2, _ => 1 };
            return probScore * impactScore; // 1-16 scale
        }

        private SKColor GetRiskColor()
        {
            int level = GetRiskLevel();
            if (level >= 12) return new SKColor(0xE5, 0x39, 0x35); // Red - Critical
            if (level >= 6) return new SKColor(0xFF, 0x98, 0x00);  // Orange - High
            if (level >= 3) return new SKColor(0xFF, 0xEB, 0x3B);  // Yellow - Medium
            return new SKColor(0x8B, 0xC3, 0x4A);                   // Green - Low
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;
            float triHeight = r.Height * 0.866f;

            // Input port at bottom left
            if (InConnectionPoints.Count > 0)
            {
                var pt = InConnectionPoints[0];
                pt.Center = new SKPoint(r.Left + r.Width * 0.25f, r.Bottom);
                pt.Position = new SKPoint(pt.Center.X, r.Bottom + PortRadius);
                pt.Bounds = new SKRect(pt.Center.X - PortRadius, pt.Center.Y - PortRadius, pt.Center.X + PortRadius, pt.Center.Y + PortRadius);
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }

            // Output port at bottom right
            if (OutConnectionPoints.Count > 0)
            {
                var pt = OutConnectionPoints[0];
                pt.Center = new SKPoint(r.Right - r.Width * 0.25f, r.Bottom);
                pt.Position = new SKPoint(pt.Center.X, r.Bottom + PortRadius);
                pt.Bounds = new SKRect(pt.Center.X - PortRadius, pt.Center.Y - PortRadius, pt.Center.X + PortRadius, pt.Center.Y + PortRadius);
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float triHeight = r.Height * 0.75f;
            float triTop = r.Top + 8;

            // Triangle (warning symbol)
            using var path = new SKPath();
            path.MoveTo(r.MidX, triTop);
            path.LineTo(r.Left + 8, triTop + triHeight);
            path.LineTo(r.Right - 8, triTop + triHeight);
            path.Close();

            SKColor riskColor = GetRiskColor();
            using var fill = new SKPaint { Color = riskColor.WithAlpha(100), IsAntialias = true };
            using var stroke = new SKPaint { Color = riskColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw warning exclamation mark
            float warnX = r.MidX;
            float warnY = triTop + triHeight * 0.3f;
            using var warnPaint = new SKPaint { Color = riskColor, IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round };
            canvas.DrawLine(warnX, warnY, warnX, warnY + 20, warnPaint);
            canvas.DrawCircle(warnX, warnY + 28, 2.5f, warnPaint);

            // Draw risk description
            using var nameFont = new SKFont(SKTypeface.Default, 11);
            float descY = triTop + triHeight + 12;
            canvas.DrawText(RiskDescription, r.MidX, descY, SKTextAlign.Center, nameFont, text);

            // Draw probability x impact
            using var detailFont = new SKFont(SKTypeface.Default, 9);
            using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
            string details = $"P: {Probability} | I: {Impact}";
            canvas.DrawText(details, r.MidX, descY + 14, SKTextAlign.Center, detailFont, grayText);

            DrawPorts(canvas);
        }
    }
}
