using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML representation of an output automation node.
    /// Displays as a rounded rectangle with output-specific visual cues.
    /// </summary>
    public class UMLOutputNode : UMLControl
    {
        /// <summary>
        /// Gets or sets the output type (File, Database, API, etc.).
        /// </summary>
    private string _outputType = "Output";
    public string OutputType { get => _outputType; set { if (_outputType == value) return; _outputType = value ?? string.Empty; if (NodeProperties.TryGetValue("OutputType", out var pi)) pi.ParameterCurrentValue = _outputType; InvalidateVisual(); } }

        /// <summary>
        /// Gets or sets the output destination or filename.
        /// </summary>
    private string _outputDestination = "";
    public string OutputDestination { get => _outputDestination; set { if (_outputDestination == value) return; _outputDestination = value ?? string.Empty; if (NodeProperties.TryGetValue("OutputDestination", out var pi)) pi.ParameterCurrentValue = _outputDestination; InvalidateVisual(); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLOutputNode"/> class.
        /// </summary>
        public UMLOutputNode()
        {
            Width = 150;
            Height = 70;
            Name = "OutputNode";
            Stereotype = "<<output>>";
            BackgroundColor = SKColors.LightSalmon;

            // Seed NodeProperties
            NodeProperties["OutputType"] = new ParameterInfo { ParameterName = "OutputType", ParameterType = typeof(string), DefaultParameterValue = _outputType, ParameterCurrentValue = _outputType, Description = "Output type (File, DB, API)" };
            NodeProperties["OutputDestination"] = new ParameterInfo { ParameterName = "OutputDestination", ParameterType = typeof(string), DefaultParameterValue = _outputDestination, ParameterCurrentValue = _outputDestination, Description = "Destination or filename" };
        }

        /// <summary>
        /// Draws the output node content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawUMLContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw rounded rectangle background
            using (var paint = new SKPaint())
            {
                paint.Color = BackgroundColor;
                paint.IsAntialias = true;

                // Absolute rect
                var rect = new SKRect(X + 5, Y + 5, X + Width - 5, Y + Height - 5);
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
                using var font = new SKFont(SKTypeface.Default, 9);
                using var textPaint = new SKPaint { IsAntialias = true, Color = TextColor };
                canvas.DrawText(Stereotype, 10, 18, font, textPaint);
            }

            // Draw output type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 11);
            using var typePaint = new SKPaint { IsAntialias = true, Color = TextColor };
            canvas.DrawText(OutputType, 10, 38, typeFont, typePaint);

            // Draw output destination if present
            if (!string.IsNullOrEmpty(OutputDestination))
            {
                using var destFont = new SKFont(SKTypeface.Default, 8);
                using var destPaint = new SKPaint { IsAntialias = true, Color = TextColor };

                // Truncate long destinations
                var dest = OutputDestination.Length > 25 ?
                    OutputDestination.Substring(0, 22) + "..." :
                    OutputDestination;

                canvas.DrawText(dest, 10, 52, destFont, destPaint);
            }

            // Draw output arrow icon
            DrawOutputArrow(canvas, X + Width - 30, Y + Height / 2 - 8);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws an output arrow icon indicating data flow direction.
        /// </summary>
        private void DrawOutputArrow(SKCanvas canvas, float x, float y)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.DarkRed;
                paint.StrokeWidth = 2;
                paint.IsAntialias = true;
                paint.StrokeCap = SKStrokeCap.Round;

                // Draw arrow shaft
                canvas.DrawLine(x, y + 8, x + 16, y + 8, paint);

                // Draw arrow head
                canvas.DrawLine(x + 13, y + 4, x + 16, y + 8, paint);
                canvas.DrawLine(x + 13, y + 12, x + 16, y + 8, paint);

                // Draw small box at the end to represent output destination
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.DarkRed.WithAlpha(128);
                canvas.DrawRect(new SKRect(x + 18, y + 6, x + 22, y + 10), paint);
            }
        }
    }
}