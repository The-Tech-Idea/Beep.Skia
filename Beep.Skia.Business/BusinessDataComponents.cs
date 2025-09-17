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
    }

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
    }

    /// <summary>
    /// Represents a data store in a business process diagram.
    /// Displayed as an open box.
    /// </summary>
    public class DataStore : BusinessControl
    {
        public DataStore()
        {
            Width = 90;
            Height = 70;
            Name = "Data Store";
            ComponentType = BusinessComponentType.DataStore;
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

            // Create open box path
            using var path = new SKPath();
            float depth = 10;

            // Front face
            path.MoveTo(X, Y + depth);
            path.LineTo(X + Width - depth, Y + depth);
            path.LineTo(X + Width - depth, Y + Height);
            path.LineTo(X, Y + Height);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Top face
            using var topPath = new SKPath();
            topPath.MoveTo(X, Y + depth);
            topPath.LineTo(X + depth, Y);
            topPath.LineTo(X + Width, Y);
            topPath.LineTo(X + Width - depth, Y + depth);
            topPath.Close();

            canvas.DrawPath(topPath, fillPaint);
            canvas.DrawPath(topPath, borderPaint);

            // Right face
            using var rightPath = new SKPath();
            rightPath.MoveTo(X + Width - depth, Y + depth);
            rightPath.LineTo(X + Width, Y);
            rightPath.LineTo(X + Width, Y + Height - depth);
            rightPath.LineTo(X + Width - depth, Y + Height);
            rightPath.Close();

            canvas.DrawPath(rightPath, fillPaint);
            canvas.DrawPath(rightPath, borderPaint);
        }
    }

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