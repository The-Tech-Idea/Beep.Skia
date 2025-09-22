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
        }
    }
}