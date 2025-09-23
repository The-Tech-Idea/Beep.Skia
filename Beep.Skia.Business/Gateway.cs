using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a gateway in a business process diagram.
    /// Displayed as a diamond with a plus sign inside.
    /// </summary>
    public class Gateway : BusinessControl
    {
        public Gateway()
        {
            Width = 70;
            Height = 70;
            Name = "Gateway";
            ComponentType = BusinessComponentType.Gateway;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
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

            // Create diamond path
            using var path = new SKPath();
            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;

            path.MoveTo(centerX, Y + 5); // Top
            path.LineTo(X + Width - 5, centerY); // Right
            path.LineTo(centerX, Y + Height - 5); // Bottom
            path.LineTo(X + 5, centerY); // Left
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Draw plus sign
            using var plusPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float plusSize = 12;
            // Horizontal line
            canvas.DrawLine(centerX - plusSize, centerY, centerX + plusSize, centerY, plusPaint);
            // Vertical line
            canvas.DrawLine(centerX, centerY - plusSize, centerX, centerY + plusSize, plusPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            // Override to not draw text in the center (due to plus sign)
            if (string.IsNullOrEmpty(Name))
                return;

            using var font = new SKFont(SKTypeface.Default, 10);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 15; // Draw text below the shape

            canvas.DrawText(Name, centerX, textY, SKTextAlign.Center, font, paint);
        }
    }
}