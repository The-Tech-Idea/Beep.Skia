using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML representation of a data source automation node.
    /// Displays as a cylinder shape with data source-specific visual cues.
    /// </summary>
    public class UMLDataSourceNode : UMLControl
    {
        /// <summary>
        /// Gets or sets the data source type (Database, API, File, etc.).
        /// </summary>
        public string DataSourceType { get; set; } = "Database";

        /// <summary>
        /// Gets or sets the data source name or endpoint.
        /// </summary>
        public string DataSourceName { get; set; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLDataSourceNode"/> class.
        /// </summary>
        public UMLDataSourceNode()
        {
            Width = 160;
            Height = 90;
            Name = "DataSourceNode";
            Stereotype = "<<dataSource>>";
            BackgroundColor = SKColors.LightCyan;
        }

        /// <summary>
        /// Draws the data source node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw cylinder shape (database symbol)
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                // Draw main cylinder body
                var rect = new SKRect(8, 15, Width - 8, Height - 15);
                canvas.DrawRect(rect, paint);

                // Draw top ellipse
                var topEllipseRect = new SKRect(8, 5, Width - 8, 25);
                canvas.DrawOval(topEllipseRect, paint);

                // Draw bottom ellipse
                var bottomEllipseRect = new SKRect(8, Height - 25, Width - 8, Height - 5);
                canvas.DrawOval(bottomEllipseRect, paint);

                // Draw border
                paint.Color = BorderColor;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = BorderThickness;

                canvas.DrawRect(rect, paint);
                canvas.DrawArc(topEllipseRect, 0, 180, false, paint);
                canvas.DrawArc(bottomEllipseRect, 180, 180, false, paint);
            }

            // Draw stereotype
            if (!string.IsNullOrEmpty(Stereotype))
            {
                using var font = new SKFont(SKTypeface.Default, 9);
                using var textPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(Stereotype, 10, 18, font, textPaint);
            }

            // Draw data source type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 11);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            canvas.DrawText(DataSourceType, 10, 40, typeFont, typePaint);

            // Draw data source name if present
            if (!string.IsNullOrEmpty(DataSourceName))
            {
                using var nameFont = new SKFont(SKTypeface.Default, 9);
                using var namePaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(DataSourceName, 10, 58, nameFont, namePaint);
            }

            // Draw database icon
            DrawDatabaseIcon(canvas, Width - 25, 20);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws connection points positioned around the cylinder shape.
        /// </summary>
        protected override void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            // Position connection points around the cylinder
            var points = new List<(SKPoint position, SKColor color)>
            {
                // Top ellipse center (input)
                (new SKPoint(Width / 2, 15), SKColors.Blue),
                // Bottom ellipse center (input)
                (new SKPoint(Width / 2, Height - 15), SKColors.Blue),
                // Left side of body (output)
                (new SKPoint(8, Height / 2), SKColors.Green),
                // Right side of body (output)
                (new SKPoint(Width - 8, Height / 2), SKColors.Green)
            };

            foreach (var (position, color) in points)
            {
                DrawConnectionPoint(canvas, position, color);
            }
        }

        /// <summary>
        /// Draws a single connection point.
        /// </summary>
        private void DrawConnectionPoint(SKCanvas canvas, SKPoint position, SKColor color)
        {
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            canvas.DrawCircle(position.X, position.Y, 6, paint);
            canvas.DrawCircle(position.X, position.Y, 6, borderPaint);
        }

        /// <summary>
        /// Draws a small database cylinder icon.
        /// </summary>
        private void DrawDatabaseIcon(SKCanvas canvas, float x, float y)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.DarkCyan;
                paint.StrokeWidth = 1;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Stroke;

                // Draw small cylinder
                canvas.DrawRect(new SKRect(x, y + 3, x + 12, y + 8), paint);
                canvas.DrawArc(new SKRect(x, y, x + 12, y + 6), 0, 180, false, paint);
                canvas.DrawArc(new SKRect(x, y + 6, x + 12, y + 12), 180, 180, false, paint);
            }
        }
    }
}