using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a task in a business process diagram.
    /// Displayed as a rounded rectangle.
    /// </summary>
    public class BusinessTask : BusinessControl
    {
        public BusinessTask()
        {
            Width = 120;
            Height = 60;
            Name = "Task";
            ComponentType = BusinessComponentType.Task;
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

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 12, 12, fillPaint);
            canvas.DrawRoundRect(rect, 12, 12, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Rounded rect: 1 in / 1 out, vertical segments
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}