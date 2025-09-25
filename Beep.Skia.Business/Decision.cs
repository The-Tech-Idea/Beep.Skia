using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a decision point in a business process diagram.
    /// Displayed as a diamond shape.
    /// </summary>
    public class Decision : BusinessControl
    {
        public Decision()
        {
            Width = 80;
            Height = 80;
            Name = "Decision";
            ComponentType = BusinessComponentType.Decision;
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
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Create diamond path
            using var path = new SKPath();
            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float halfWidth = Width / 2;
            float halfHeight = Height / 2;

            path.MoveTo(centerX, Y); // Top
            path.LineTo(X + Width, centerY); // Right
            path.LineTo(centerX, Y + Height); // Bottom
            path.LineTo(X, centerY); // Left
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // For a diamond, avoid top/bottom sharp corners by adding generous insets.
            LayoutPortsVerticalSegments(topInset: Height * 0.25f, bottomInset: Height * 0.25f);
        }
    }
}