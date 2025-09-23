using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML representation of a trigger automation node.
    /// Displays as a rounded rectangle with trigger-specific visual cues.
    /// </summary>
    public class UMLTriggerNode : UMLControl
    {
        /// <summary>
        /// Gets or sets the trigger type (Event, Schedule, Manual, etc.).
        /// </summary>
        public string TriggerType { get; set; } = "Event";

        /// <summary>
        /// Gets or sets the trigger condition or schedule.
        /// </summary>
        public string TriggerCondition { get; set; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLTriggerNode"/> class.
        /// </summary>
        public UMLTriggerNode()
        {
            Width = 160;
            Height = 90;
            Name = "TriggerNode";
            Stereotype = "<<trigger>>";
            BackgroundColor = SKColors.LightGreen;
            DisplayText = "Trigger";
            TextPosition = TextPosition.Inside;
            ShowDisplayText = true;
        }

        /// <summary>
        /// Draws the trigger node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw rounded rectangle background
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                var rect = new SKRect(2, 2, Width - 2, Height - 2);
                canvas.DrawRoundRect(rect, 8, 8, paint);

                // Draw border
                paint.Color = BorderColor;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = BorderThickness;
                canvas.DrawRoundRect(rect, 8, 8, paint);
            }

            // Draw stereotype
            if (!string.IsNullOrEmpty(Stereotype))
            {
                using var font = new SKFont(SKTypeface.Default, 10);
                using var textPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                var stereotypeWidth = font.MeasureText(Stereotype);
                canvas.DrawText(Stereotype, (Width - stereotypeWidth) / 2, 18, font, textPaint);
            }

            // Draw trigger type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 12);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            canvas.DrawText(TriggerType, 8, 35, typeFont, typePaint);

            // Draw trigger condition if present
            if (!string.IsNullOrEmpty(TriggerCondition))
            {
                using var condFont = new SKFont(SKTypeface.Default, 10);
                using var condPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(TriggerCondition, 8, 55, condFont, condPaint);
            }

            // Draw lightning bolt icon to represent trigger
            DrawLightningBolt(canvas, Width - 25, 10);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws a small lightning bolt icon.
        /// </summary>
        private void DrawLightningBolt(SKCanvas canvas, float x, float y)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.Yellow;
                paint.StrokeWidth = 2;
                paint.IsAntialias = true;

                var path = new SKPath();
                path.MoveTo(x + 5, y);
                path.LineTo(x + 2, y + 3);
                path.LineTo(x + 6, y + 3);
                path.LineTo(x + 1, y + 8);
                path.LineTo(x + 8, y + 3);
                path.LineTo(x + 3, y + 3);
                path.Close();

                canvas.DrawPath(path, paint);
            }
        }
    }
}