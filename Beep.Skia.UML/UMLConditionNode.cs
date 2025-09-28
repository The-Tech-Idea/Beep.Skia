using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML representation of a condition automation node.
    /// Displays as a diamond shape with condition-specific visual cues.
    /// </summary>
    public class UMLConditionNode : UMLControl
    {
        /// <summary>
        /// Gets or sets the condition expression.
        /// </summary>
        public string ConditionExpression { get; set; } = "";

        /// <summary>
        /// Gets or sets the condition type (Boolean, Comparison, Complex).
        /// </summary>
        public string ConditionType { get; set; } = "Boolean";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLConditionNode"/> class.
        /// </summary>
        public UMLConditionNode()
        {
            Width = 140;
            Height = 80;
            Name = "ConditionNode";
            Stereotype = "<<condition>>";
            BackgroundColor = SKColors.LightYellow;
        }

        /// <summary>
        /// Draws the condition node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            LayoutPorts();
            
            // Draw diamond shape
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                var path = new SKPath();
                path.MoveTo(Width / 2, 2);
                path.LineTo(Width - 2, Height / 2);
                path.LineTo(Width / 2, Height - 2);
                path.LineTo(2, Height / 2);
                path.Close();

                canvas.DrawPath(path, paint);

                // Draw border
                paint.Color = BorderColor;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = BorderThickness;
                canvas.DrawPath(path, paint);
            }

            // Draw stereotype
            if (!string.IsNullOrEmpty(Stereotype))
            {
                using var font = new SKFont(SKTypeface.Default, 9);
                using var textPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(Stereotype, Width / 2 - 25, 18, font, textPaint);
            }

            // Draw condition type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 10);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            var typeWidth = typeFont.MeasureText(ConditionType);
            canvas.DrawText(ConditionType, (Width - typeWidth) / 2, Height / 2 - 5, typeFont, typePaint);

            // Draw condition expression if present
            if (!string.IsNullOrEmpty(ConditionExpression))
            {
                using var exprFont = new SKFont(SKTypeface.Default, 8);
                using var exprPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                var exprWidth = exprFont.MeasureText(ConditionExpression);
                if (exprWidth > Width - 10)
                {
                    // Truncate if too long
                    var truncated = ConditionExpression.Length > 15 ?
                        ConditionExpression.Substring(0, 12) + "..." : ConditionExpression;
                    var truncWidth = exprFont.MeasureText(truncated);
                    canvas.DrawText(truncated, (Width - truncWidth) / 2, Height / 2 + 10, exprFont, exprPaint);
                }
                else
                {
                    canvas.DrawText(ConditionExpression, (Width - exprWidth) / 2, Height / 2 + 10, exprFont, exprPaint);
                }
            }

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws connection points positioned at the diamond's corners.
        /// </summary>
        protected override void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            // Position connection points at the diamond's corners
            var points = new List<(SKPoint position, SKColor color)>
            {
                (new SKPoint(Width / 2, 2), SKColors.Blue),      // Top point (input)
                (new SKPoint(Width - 2, Height / 2), SKColors.Green), // Right point (output)
                (new SKPoint(Width / 2, Height - 2), SKColors.Blue),  // Bottom point (input)
                (new SKPoint(2, Height / 2), SKColors.Green)     // Left point (output)
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
        /// Gets the effective text positioning bounds for the diamond shape.
        /// For diamond shapes, we adjust the bounds to account for the diamond's geometry
        /// so text positioning (Above, Below, Left, Right) is relative to the diamond's points.
        /// </summary>
        /// <returns>A rectangle defining the effective bounds for text positioning.</returns>
        protected override SKRect GetTextPositioningBounds()
        {
            // For diamond shape, adjust bounds to be relative to the diamond's points
            // The diamond extends to the corners, so we use the full rectangular bounds
            // but components can override positioning logic if needed
            return new SKRect(X, Y, X + Width, Y + Height);
        }
    }
}