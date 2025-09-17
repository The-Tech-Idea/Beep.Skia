using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a person in an organizational chart.
    /// Displayed as a circle with a person icon.
    /// </summary>
    public class Person : BusinessControl
    {
        public string PersonName { get; set; } = "Person";
        public string Title { get; set; } = "";

        public Person()
        {
            Width = 80;
            Height = 80;
            Name = "Person";
            ComponentType = BusinessComponentType.Person;
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

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - 2;

            // Draw circle background
            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, borderPaint);

            // Draw person icon
            DrawPersonIcon(canvas, centerX, centerY, radius * 0.7f);
        }

        private void DrawPersonIcon(SKCanvas canvas, float centerX, float centerY, float iconSize)
        {
            using var iconPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            // Head (smaller circle)
            float headRadius = iconSize * 0.25f;
            float headCenterY = centerY - iconSize * 0.3f;
            canvas.DrawCircle(centerX, headCenterY, headRadius, iconPaint);

            // Body (arc for shoulders)
            float bodyTop = headCenterY + headRadius + 5;
            float bodyRadius = iconSize * 0.4f;
            var bodyRect = new SKRect(centerX - bodyRadius, bodyTop, centerX + bodyRadius, bodyTop + bodyRadius * 1.2f);
            
            using var bodyPath = new SKPath();
            bodyPath.AddArc(bodyRect, 30, 120); // Arc from 30 to 150 degrees
            canvas.DrawPath(bodyPath, iconPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(PersonName))
                return;

            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var titleFont = new SKFont(SKTypeface.Default, 9);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float nameY = Y + Height + 15;
            float titleY = nameY + 15;

            canvas.DrawText(PersonName, centerX, nameY, SKTextAlign.Center, nameFont, paint);
            
            if (!string.IsNullOrEmpty(Title))
            {
                canvas.DrawText(Title, centerX, titleY, SKTextAlign.Center, titleFont, paint);
            }
        }
    }

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

    /// <summary>
    /// Represents a role in an organizational chart.
    /// Displayed as a badge shape.
    /// </summary>
    public class Role : BusinessControl
    {
        public string RoleName { get; set; } = "Role";

        public Role()
        {
            Width = 100;
            Height = 50;
            Name = "Role";
            ComponentType = BusinessComponentType.Role;
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

            // Create badge path with notched corners
            using var path = new SKPath();
            float notchSize = 8;

            path.MoveTo(X + notchSize, Y);
            path.LineTo(X + Width - notchSize, Y);
            path.LineTo(X + Width, Y + notchSize);
            path.LineTo(X + Width, Y + Height - notchSize);
            path.LineTo(X + Width - notchSize, Y + Height);
            path.LineTo(X + notchSize, Y + Height);
            path.LineTo(X, Y + Height - notchSize);
            path.LineTo(X, Y + notchSize);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(RoleName))
                return;

            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2 + 4;

            canvas.DrawText(RoleName, centerX, centerY, SKTextAlign.Center, font, paint);
        }
    }

    /// <summary>
    /// Represents a system in a business process diagram.
    /// Displayed as a server/system shape.
    /// </summary>
    public class BusinessSystem : BusinessControl
    {
        public string SystemName { get; set; } = "System";

        public BusinessSystem()
        {
            Width = 100;
            Height = 120;
            Name = "System";
            ComponentType = BusinessComponentType.System;
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

            // Draw server chassis
            var serverRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(serverRect, 6, 6, fillPaint);
            canvas.DrawRoundRect(serverRect, 6, 6, borderPaint);

            // Draw server details
            DrawServerDetails(canvas);
        }

        private void DrawServerDetails(SKCanvas canvas)
        {
            using var detailPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw horizontal dividers for server units
            for (int i = 1; i < 4; i++)
            {
                float dividerY = Y + (Height / 4) * i;
                canvas.DrawLine(X + 5, dividerY, X + Width - 5, dividerY, detailPaint);
            }

            // Draw power indicators (small circles)
            for (int i = 0; i < 3; i++)
            {
                float indicatorY = Y + 15 + (i * Height / 4);
                float indicatorX = X + Width - 15;
                
                using var indicatorPaint = new SKPaint
                {
                    Color = i == 0 ? SKColors.Green : SKColors.Gray,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                canvas.DrawCircle(indicatorX, indicatorY, 3, indicatorPaint);
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(SystemName))
                return;

            using var font = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 15;

            canvas.DrawText(SystemName, centerX, textY, SKTextAlign.Center, font, paint);
        }
    }
}