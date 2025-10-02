using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML representation of an action automation node.
    /// Displays as a rectangle with action-specific visual cues.
    /// </summary>
    public class UMLActionNode : UMLControl
    {
        /// <summary>
        /// Gets or sets the action type (API, Database, File, etc.).
        /// </summary>
        public string ActionType { get; set; } = "Generic";

        /// <summary>
        /// Gets or sets the action description.
        /// </summary>
        public string ActionDescription { get; set; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLActionNode"/> class.
        /// </summary>
        public UMLActionNode()
        {
            Width = 160;
            Height = 90;
            Name = "ActionNode";
            Stereotype = "<<action>>";
            BackgroundColor = SKColors.LightBlue;
        }

        /// <summary>
        /// Draws the action node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawUMLContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw rectangle background
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                var rect = new SKRect(2, 2, Width - 2, Height - 2);
                canvas.DrawRect(rect, paint);

                // Draw border
                paint.Color = BorderColor;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = BorderThickness;
                canvas.DrawRect(rect, paint);
            }

            // Draw stereotype
            if (!string.IsNullOrEmpty(Stereotype))
            {
                using var font = new SKFont(SKTypeface.Default, 10);
                using var textPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                var stereotypeWidth = font.MeasureText(Stereotype);
                canvas.DrawText(Stereotype, (Width - stereotypeWidth) / 2, 18, font, textPaint);
            }

            // Draw action type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 12);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            canvas.DrawText(ActionType, 8, 35, typeFont, typePaint);

            // Draw action description if present
            if (!string.IsNullOrEmpty(ActionDescription))
            {
                using var descFont = new SKFont(SKTypeface.Default, 10);
                using var descPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(ActionDescription, 8, 55, descFont, descPaint);
            }

            // Draw gear icon to represent action
            DrawGearIcon(canvas, Width - 20, Height - 20);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws a small gear icon.
        /// </summary>
        private void DrawGearIcon(SKCanvas canvas, float centerX, float centerY)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.Gray;
                paint.StrokeWidth = 1;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Stroke;

                float radius = 8;
                canvas.DrawCircle(centerX, centerY, radius, paint);

                // Draw gear teeth
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45 * (float)Math.PI / 180;
                    float x1 = centerX + (radius + 2) * (float)Math.Cos(angle);
                    float y1 = centerY + (radius + 2) * (float)Math.Sin(angle);
                    float x2 = centerX + (radius - 2) * (float)Math.Cos(angle);
                    float y2 = centerY + (radius - 2) * (float)Math.Sin(angle);
                    canvas.DrawLine(x1, y1, x2, y2, paint);
                }
            }
        }
    }
}