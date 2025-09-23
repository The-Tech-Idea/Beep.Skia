using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a condition builder in a business rule diagram.
    /// Displayed as a question mark in a rounded rectangle.
    /// </summary>
    public class ConditionBuilder : BusinessControl
    {
        public string ConditionText { get; set; } = "Condition";
        public ConditionType ConditionType { get; set; } = ConditionType.Simple;
        public bool IsValid { get; set; } = true;

        public ConditionBuilder()
        {
            Width = 120;
            Height = 60;
            Name = "Condition";
            ComponentType = BusinessComponentType.Decision;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = IsValid ? BackgroundColor : SKColors.LightCoral,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = IsValid ? BorderColor : SKColors.DarkRed,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);

            // Draw question mark icon
            DrawQuestionMark(canvas);
        }

        private void DrawQuestionMark(SKCanvas canvas)
        {
            using var iconPaint = new SKPaint
            {
                Color = IsValid ? BorderColor : SKColors.DarkRed,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float iconSize = Math.Min(Width, Height) * 0.4f;

            // Draw question mark path
            using var path = new SKPath();
            float startX = centerX - iconSize * 0.3f;
            float startY = centerY - iconSize * 0.4f;

            // Question mark curve
            path.MoveTo(startX, startY);
            path.CubicTo(
                startX + iconSize * 0.2f, startY - iconSize * 0.3f,
                startX + iconSize * 0.6f, startY - iconSize * 0.3f,
                startX + iconSize * 0.6f, startY
            );
            path.CubicTo(
                startX + iconSize * 0.6f, startY + iconSize * 0.2f,
                startX + iconSize * 0.4f, startY + iconSize * 0.4f,
                startX + iconSize * 0.4f, startY + iconSize * 0.4f
            );

            // Dot
            path.AddCircle(startX + iconSize * 0.4f, startY + iconSize * 0.6f, 2);

            canvas.DrawPath(path, iconPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(ConditionText))
                return;

            using var font = new SKFont(SKTypeface.Default, 9);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 12;

            canvas.DrawText(ConditionText, centerX, textY, SKTextAlign.Center, font, paint);
        }
    }
}