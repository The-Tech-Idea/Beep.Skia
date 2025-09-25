using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a start event in a business process diagram.
    /// Displayed as a thin-bordered circle.
    /// </summary>
    public class StartEvent : BusinessControl
    {
        public StartEvent()
        {
            Width = 60;
            Height = 60;
            Name = "Start";
            ComponentType = BusinessComponentType.StartEvent;
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
                Color = MaterialColors.Outline,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - 2;

            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Place the single output on the ellipse perimeter to the right; no inputs.
            EnsurePortCounts(0, 1);
            LayoutPortsOnEllipse(topInset: 4f, bottomInset: 4f, outwardOffset: 2f);
        }
    }
}