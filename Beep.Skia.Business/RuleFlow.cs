using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a rule flow connector in a business rule diagram.
    /// Displayed as a curved arrow with rule evaluation indicators.
    /// </summary>
    public class RuleFlow : BusinessControl
    {
        public string FlowLabel { get; set; } = "";
        public FlowDirection Direction { get; set; } = FlowDirection.True;
        public bool IsActive { get; set; } = false;

        public RuleFlow()
        {
            Width = 80;
            Height = 40;
            Name = "Rule Flow";
            ComponentType = BusinessComponentType.Task;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            // Rule flows are typically connectors, so we draw a curved arrow
            DrawCurvedArrow(canvas);
        }

        private void DrawCurvedArrow(SKCanvas canvas)
        {
            using var arrowPaint = new SKPaint
            {
                Color = IsActive ? MaterialColors.Primary : BorderColor,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float startX = X;
            float startY = Y + Height / 2;
            float endX = X + Width;
            float endY = Y + Height / 2;
            float controlY = Direction == FlowDirection.True ?
                Y - Height * 0.5f : Y + Height * 1.5f;

            // Draw curved line
            using var path = new SKPath();
            path.MoveTo(startX, startY);
            path.QuadTo((startX + endX) / 2, controlY, endX, endY);
            canvas.DrawPath(path, arrowPaint);

            // Draw arrowhead
            DrawArrowhead(canvas, endX, endY, arrowPaint);
        }

        private void DrawArrowhead(SKCanvas canvas, float x, float y, SKPaint paint)
        {
            float arrowSize = 8;
            using var arrowPath = new SKPath();

            // Calculate arrow direction based on flow
            float angle = Direction == FlowDirection.True ? -45 : 45;
            float radian = angle * (float)Math.PI / 180f;

            arrowPath.MoveTo(x, y);
            arrowPath.LineTo(
                x - arrowSize * (float)Math.Cos(radian - Math.PI / 6),
                y - arrowSize * (float)Math.Sin(radian - Math.PI / 6)
            );
            arrowPath.LineTo(
                x - arrowSize * (float)Math.Cos(radian + Math.PI / 6),
                y - arrowSize * (float)Math.Sin(radian + Math.PI / 6)
            );
            arrowPath.Close();

            using var fillPaint = new SKPaint
            {
                Color = paint.Color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawPath(arrowPath, fillPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(FlowLabel))
                return;

            using var font = new SKFont(SKTypeface.Default, 8);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Direction == FlowDirection.True ?
                Y - 5 : Y + Height + 15;

            canvas.DrawText(FlowLabel, centerX, textY, SKTextAlign.Center, font, paint);
        }

        protected override void LayoutPorts()
        {
            // RuleFlow is a visual connector; no ports by default.
            EnsurePortCounts(0, 0);
        }
    }
}