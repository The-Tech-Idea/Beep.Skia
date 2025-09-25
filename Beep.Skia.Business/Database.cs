using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a database in a business process diagram.
    /// Displayed as a cylinder shape.
    /// </summary>
    public class Database : BusinessControl
    {
        public Database()
        {
            Width = 80;
            Height = 100;
            Name = "Database";
            ComponentType = BusinessComponentType.Database;
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

            float ellipseHeight = 20;
            float centerX = X + Width / 2;

            // Top ellipse
            var topEllipse = new SKRect(X, Y, X + Width, Y + ellipseHeight);
            canvas.DrawOval(topEllipse, fillPaint);
            canvas.DrawOval(topEllipse, borderPaint);

            // Cylinder body
            var bodyRect = new SKRect(X, Y + ellipseHeight / 2, X + Width, Y + Height - ellipseHeight / 2);
            canvas.DrawRect(bodyRect, fillPaint);

            // Side lines
            canvas.DrawLine(X, Y + ellipseHeight / 2, X, Y + Height - ellipseHeight / 2, borderPaint);
            canvas.DrawLine(X + Width, Y + ellipseHeight / 2, X + Width, Y + Height - ellipseHeight / 2, borderPaint);

            // Bottom ellipse
            var bottomEllipse = new SKRect(X, Y + Height - ellipseHeight, X + Width, Y + Height);
            canvas.DrawOval(bottomEllipse, fillPaint);
            canvas.DrawOval(bottomEllipse, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Cylinder: use vertical segment layout; 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }
    }
}