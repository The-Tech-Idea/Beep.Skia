using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents external data in a business process diagram.
    /// Displayed as a parallelogram.
    /// </summary>
    public class ExternalData : BusinessControl
    {
        public ExternalData()
        {
            Width = 110;
            Height = 60;
            Name = "External Data";
            ComponentType = BusinessComponentType.ExternalData;
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

            // Create parallelogram path
            using var path = new SKPath();
            float skew = 15;

            path.MoveTo(X + skew, Y);
            path.LineTo(X + Width, Y);
            path.LineTo(X + Width - skew, Y + Height);
            path.LineTo(X, Y + Height);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);
        }
    }
}