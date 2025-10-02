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
    private string _transformType = "Transform";
    public string TransformType { get => _transformType; set { if (_transformType == value) return; _transformType = value ?? string.Empty; if (NodeProperties.TryGetValue("TransformType", out var pi)) pi.ParameterCurrentValue = _transformType; InvalidateVisual(); } }

        /// <summary>
        /// Gets or sets the transform description or expression.
        /// </summary>
    private string _transformDescription = "";
    public string TransformDescription { get => _transformDescription; set { if (_transformDescription == value) return; _transformDescription = value ?? string.Empty; if (NodeProperties.TryGetValue("TransformDescription", out var pi)) pi.ParameterCurrentValue = _transformDescription; InvalidateVisual(); } }

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

            // Seed NodeProperties
            NodeProperties["TransformType"] = new ParameterInfo { ParameterName = "TransformType", ParameterType = typeof(string), DefaultParameterValue = _transformType, ParameterCurrentValue = _transformType, Description = "Operation type (Filter, Sort, etc.)" };
            NodeProperties["TransformDescription"] = new ParameterInfo { ParameterName = "TransformDescription", ParameterType = typeof(string), DefaultParameterValue = _transformDescription, ParameterCurrentValue = _transformDescription, Description = "Short description/expression" };
        }

        /// <summary>
        /// Draws the transform node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawUMLContent(SKCanvas canvas, DrawingContext context)
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

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws connection points positioned at the hexagon's flat sides.
        /// </summary>
        protected override void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            var centerX = Width / 2;
            var centerY = Height / 2;
            var radius = Width / 2 - 5;

            // Position connection points at the midpoints of each hexagon side
            var points = new List<(SKPoint position, SKColor color)>();

            for (int i = 0; i < 6; i++)
            {
                // Calculate the midpoint of each side
                var angle1 = i * Math.PI / 3;
                var angle2 = ((i + 1) % 6) * Math.PI / 3;

                var x1 = centerX + radius * Math.Cos(angle1);
                var y1 = centerY + radius * Math.Sin(angle1);
                var x2 = centerX + radius * Math.Cos(angle2);
                var y2 = centerY + radius * Math.Sin(angle2);

                var midX = (x1 + x2) / 2;
                var midY = (y1 + y2) / 2;

                // Alternate between input (blue) and output (green) for visual distinction
                var color = (i % 2 == 0) ? SKColors.Blue : SKColors.Green;
                points.Add((new SKPoint((float)midX, (float)midY), color));
            }

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