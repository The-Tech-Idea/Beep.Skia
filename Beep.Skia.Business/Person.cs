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

        protected override void LayoutPorts()
        {
            // Circle profile: use ellipse perimeter placement, 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsOnEllipse(topInset: 4f, bottomInset: 4f, outwardOffset: 2f);
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
}