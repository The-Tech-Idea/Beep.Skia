using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML representation of a transform automation node.
    /// Displays as a hexagon shape with transform-specific visual cues.
    /// </summary>
    public class UMLTransformNode : UMLControl
    {
        /// <summary>
        /// Gets or sets the transform operation type (Filter, Sort, Aggregate, etc.).
        /// </summary>
        public string TransformType { get; set; } = "Transform";

        /// <summary>
        /// Gets or sets the transform description or expression.
        /// </summary>
        public string TransformDescription { get; set; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLTransformNode"/> class.
        /// </summary>
        public UMLTransformNode()
        {
            Width = 140;
            Height = 80;
            Name = "TransformNode";
            Stereotype = "<<transform>>";
            BackgroundColor = SKColors.LightGreen;
        }

        /// <summary>
        /// Draws the transform node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw hexagon shape
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                var path = CreateHexagonPath();
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
                canvas.DrawText(Stereotype, 10, 18, font, textPaint);
            }

            // Draw transform type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 11);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            canvas.DrawText(TransformType, 10, 38, typeFont, typePaint);

            // Draw transform description if present
            if (!string.IsNullOrEmpty(TransformDescription))
            {
                using var descFont = new SKFont(SKTypeface.Default, 8);
                using var descPaint = new SKPaint { IsAntialias = true, Color = TextColor };

                // Truncate long descriptions
                var desc = TransformDescription.Length > 20 ?
                    TransformDescription.Substring(0, 17) + "..." :
                    TransformDescription;

                canvas.DrawText(desc, 10, 55, descFont, descPaint);
            }

            // Draw transform arrows
            DrawTransformArrows(canvas);
        }

        /// <summary>
        /// Creates a hexagon path for the transform node shape.
        /// </summary>
        private SKPath CreateHexagonPath()
        {
            var path = new SKPath();
            var centerX = Width / 2;
            var centerY = Height / 2;
            var radius = Width / 2 - 5;

            // Calculate hexagon vertices
            for (int i = 0; i < 6; i++)
            {
                var angle = i * Math.PI / 3;
                var x = centerX + radius * Math.Cos(angle);
                var y = centerY + radius * Math.Sin(angle);

                if (i == 0)
                    path.MoveTo((float)x, (float)y);
                else
                    path.LineTo((float)x, (float)y);
            }
            path.Close();

            return path;
        }

        /// <summary>
        /// Draws transform arrows indicating data flow transformation.
        /// </summary>
        private void DrawTransformArrows(SKCanvas canvas)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.DarkGreen;
                paint.StrokeWidth = 2;
                paint.IsAntialias = true;
                paint.StrokeCap = SKStrokeCap.Round;

                // Draw curved arrows inside the hexagon
                var centerX = Width / 2;
                var centerY = Height / 2;

                // Left to right arrow
                canvas.DrawLine(centerX - 15, centerY - 8, centerX + 15, centerY - 8, paint);
                canvas.DrawLine(centerX + 12, centerY - 12, centerX + 15, centerY - 8, paint);
                canvas.DrawLine(centerX + 12, centerY - 4, centerX + 15, centerY - 8, paint);

                // Top to bottom arrow
                canvas.DrawLine(centerX, centerY - 15, centerX, centerY + 15, paint);
                canvas.DrawLine(centerX - 4, centerY + 12, centerX, centerY + 15, paint);
                canvas.DrawLine(centerX + 4, centerY + 12, centerX, centerY + 15, paint);
            }
        }
    }
}