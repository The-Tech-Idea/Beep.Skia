using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a business rule engine in a business process diagram.
    /// Displayed as a gear/cog shape with rule indicators.
    /// </summary>
    public class RuleEngine : BusinessControl
    {
        public string RuleSetName { get; set; } = "Rule Engine";
        public int RuleCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public RuleEngine()
        {
            Width = 100;
            Height = 100;
            Name = "Rule Engine";
            ComponentType = BusinessComponentType.Task; // Using Task as base type
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = IsActive ? BackgroundColor : BackgroundColor.WithAlpha(128),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float outerRadius = Math.Min(Width, Height) / 2 - 5;
            float innerRadius = outerRadius * 0.6f;

            // Draw gear shape
            DrawGear(canvas, centerX, centerY, outerRadius, innerRadius, fillPaint, borderPaint);

            // Draw rule indicators
            if (RuleCount > 0)
            {
                DrawRuleIndicators(canvas, centerX, centerY, innerRadius);
            }
        }

        private void DrawGear(SKCanvas canvas, float centerX, float centerY, float outerRadius, float innerRadius, SKPaint fillPaint, SKPaint borderPaint)
        {
            using var path = new SKPath();
            int teeth = 8;
            float angleStep = 360f / (teeth * 2);

            for (int i = 0; i < teeth * 2; i++)
            {
                float angle = i * angleStep;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                float radian = angle * (float)Math.PI / 180f;

                float x = centerX + radius * (float)Math.Cos(radian);
                float y = centerY + radius * (float)Math.Sin(radian);

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Draw center hole
            using var centerPaint = new SKPaint
            {
                Color = BorderColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(centerX, centerY, innerRadius * 0.3f, centerPaint);
        }

        private void DrawRuleIndicators(SKCanvas canvas, float centerX, float centerY, float radius)
        {
            using var indicatorPaint = new SKPaint
            {
                Color = MaterialColors.Tertiary,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw rule count as small circles around the center
            int displayCount = Math.Min(RuleCount, 6);
            for (int i = 0; i < displayCount; i++)
            {
                float angle = (i * 360f / displayCount) * (float)Math.PI / 180f;
                float indicatorX = centerX + radius * 0.7f * (float)Math.Cos(angle);
                float indicatorY = centerY + radius * 0.7f * (float)Math.Sin(angle);

                canvas.DrawCircle(indicatorX, indicatorY, 2, indicatorPaint);
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            using var nameFont = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var countFont = new SKFont(SKTypeface.Default, 8);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float nameY = Y + Height + 15;
            float countY = nameY + 12;

            canvas.DrawText(RuleSetName, centerX, nameY, SKTextAlign.Center, nameFont, paint);

            if (RuleCount > 0)
            {
                canvas.DrawText($"{RuleCount} rules", centerX, countY, SKTextAlign.Center, countFont, paint);
            }
        }

        protected override void LayoutPorts()
        {
            // Gear-like circular control: treat as ellipse; default 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsOnEllipse(topInset: 6f, bottomInset: 6f, outwardOffset: 2f);
        }
    }
}