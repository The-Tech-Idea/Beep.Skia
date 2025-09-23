using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a department in an organizational chart.
    /// Displayed as a rounded rectangle with department styling.
    /// </summary>
    public class Department : BusinessControl
    {
        public string DepartmentName { get; set; } = "Department";
        public int EmployeeCount { get; set; } = 0;

        public Department()
        {
            Width = 140;
            Height = 80;
            Name = "Department";
            ComponentType = BusinessComponentType.Department;
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

            // Add gradient for department styling
            var gradientColors = new SKColor[] {
                BackgroundColor.WithAlpha(255),
                BackgroundColor.WithAlpha(180)
            };
            using var gradient = SKShader.CreateLinearGradient(
                new SKPoint(X, Y),
                new SKPoint(X, Y + Height),
                gradientColors,
                null,
                SKShaderTileMode.Clamp);

            fillPaint.Shader = gradient;

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 15, 15, fillPaint);
            canvas.DrawRoundRect(rect, 15, 15, borderPaint);

            // Draw department icon in top-left corner
            DrawDepartmentIcon(canvas);
        }

        private void DrawDepartmentIcon(SKCanvas canvas)
        {
            using var iconPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float iconX = X + 8;
            float iconY = Y + 8;
            float iconSize = 16;

            // Draw building icon
            var buildingRect = new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize);
            canvas.DrawRect(buildingRect, iconPaint);

            // Draw windows
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float windowX = iconX + 3 + (j * 6);
                    float windowY = iconY + 3 + (i * 6);
                    canvas.DrawRect(windowX, windowY, windowX + 3, windowY + 3, iconPaint);
                }
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var countFont = new SKFont(SKTypeface.Default, 10);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float nameY = Y + Height / 2;
            float countY = nameY + 15;

            canvas.DrawText(DepartmentName, centerX, nameY, SKTextAlign.Center, nameFont, paint);

            if (EmployeeCount > 0)
            {
                canvas.DrawText($"{EmployeeCount} employees", centerX, countY, SKTextAlign.Center, countFont, paint);
            }
        }
    }
}