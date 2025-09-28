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
            LayoutPorts();
            
            // Base top-left for absolute drawing
            float left = X;
            float top = Y;

            // Draw cylinder shape (database symbol)
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                // Draw main cylinder body (absolute coordinates)
                var rect = new SKRect(left + 8, top + 15, left + Width - 8, top + Height - 15);
                canvas.DrawRect(rect, paint);

                // Draw top ellipse
                var topEllipseRect = new SKRect(left + 8, top + 5, left + Width - 8, top + 25);
                canvas.DrawOval(topEllipseRect, paint);

                // Draw bottom ellipse
                var bottomEllipseRect = new SKRect(left + 8, top + Height - 25, left + Width - 8, top + Height - 5);
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
                canvas.DrawText(Stereotype, left + 10, top + 18, font, textPaint);
            }

            // Draw data source type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 11);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            canvas.DrawText(DataSourceType, left + 10, top + 40, typeFont, typePaint);

            // Draw data source name if present
            if (!string.IsNullOrEmpty(DataSourceName))
            {
                using var nameFont = new SKFont(SKTypeface.Default, 9);
                using var namePaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(DataSourceName, left + 10, top + 58, nameFont, namePaint);
            }

            // Draw database icon (absolute)
            DrawDatabaseIcon(canvas, left + Width - 25, top + 20);

            // Draw connection points using persisted absolute positions
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws connection points positioned around the cylinder shape.
        /// </summary>
        protected override void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            // Use base implementation which draws from persisted ConnectionPoint positions
            base.DrawConnectionPoints(canvas, context);
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

        /// <summary>
        /// Align UML connection points to the cylinder geometry using absolute coordinates.
        /// Top/Bottom at +/-15px from edges; Left/Right at +/-8px from sides.
        /// </summary>
        protected override void LayoutPorts()
        {
            var points = InConnectionPoints; // shared list with OutConnectionPoints
            if (points == null || points.Count < 4)
            {
                base.LayoutPorts();
                return;
            }

            var topCp = points[0];
            var rightCp = points[1];
            var bottomCp = points[2];
            var leftCp = points[3];

            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            float r = topCp is { Radius: > 0 } ? topCp.Radius : 6f;

            // Top at center of top ellipse (15px from top)
            topCp.Center = new SKPoint(cx, Y + 15);
            topCp.Position = topCp.Center;
            topCp.Bounds = new SKRect(topCp.Center.X - r, topCp.Center.Y - r, topCp.Center.X + r, topCp.Center.Y + r);
            topCp.Rect = topCp.Bounds;
            topCp.Index = 0;
            topCp.Component = this;
            topCp.Type = ConnectionPointType.In; // consume data on top by convention

            // Right at center of right side (8px inset)
            rightCp.Center = new SKPoint(X + Width - 8, cy);
            rightCp.Position = rightCp.Center;
            rightCp.Bounds = new SKRect(rightCp.Center.X - r, rightCp.Center.Y - r, rightCp.Center.X + r, rightCp.Center.Y + r);
            rightCp.Rect = rightCp.Bounds;
            rightCp.Index = 1;
            rightCp.Component = this;
            rightCp.Type = ConnectionPointType.Out; // emit data to the right

            // Bottom at center of bottom ellipse (15px from bottom)
            bottomCp.Center = new SKPoint(cx, Y + Height - 15);
            bottomCp.Position = bottomCp.Center;
            bottomCp.Bounds = new SKRect(bottomCp.Center.X - r, bottomCp.Center.Y - r, bottomCp.Center.X + r, bottomCp.Center.Y + r);
            bottomCp.Rect = bottomCp.Bounds;
            bottomCp.Index = 2;
            bottomCp.Component = this;
            bottomCp.Type = ConnectionPointType.Out; // downstream out by convention

            // Left at center of left side (8px inset)
            leftCp.Center = new SKPoint(X + 8, cy);
            leftCp.Position = leftCp.Center;
            leftCp.Bounds = new SKRect(leftCp.Center.X - r, leftCp.Center.Y - r, leftCp.Center.X + r, leftCp.Center.Y + r);
            leftCp.Rect = leftCp.Bounds;
            leftCp.Index = 3;
            leftCp.Component = this;
            leftCp.Type = ConnectionPointType.In; // upstream input from left
        }
    }
}