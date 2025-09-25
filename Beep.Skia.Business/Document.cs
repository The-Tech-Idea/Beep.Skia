using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a document in a business process diagram.
    /// Displayed as a rectangle with a folded corner.
    /// </summary>
    public class Document : BusinessControl
    {
        public Document()
        {
            Width = 100;
            Height = 80;
            Name = "Document";
            ComponentType = BusinessComponentType.Document;
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

            // Create document path with folded corner
            using var path = new SKPath();
            float foldSize = 15;

            path.MoveTo(X, Y);
            path.LineTo(X + Width - foldSize, Y);
            path.LineTo(X + Width, Y + foldSize);
            path.LineTo(X + Width, Y + Height);
            path.LineTo(X, Y + Height);
            path.Close();

            // Draw main document
            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Draw fold line
            using var foldPath = new SKPath();
            foldPath.MoveTo(X + Width - foldSize, Y);
            foldPath.LineTo(X + Width - foldSize, Y + foldSize);
            foldPath.LineTo(X + Width, Y + foldSize);

            canvas.DrawPath(foldPath, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Rectangle with folded corner: vertical segments, 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}