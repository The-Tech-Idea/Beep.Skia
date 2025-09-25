using SkiaSharp;
using Beep.Skia.ETL;
using Beep.Skia.Components;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Source component that represents data sources in ETL workflows.
    /// Displays as a cylinder shape indicating data storage/retrieval.
    /// </summary>
    public class ETLSource : ETLControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLSource"/> class.
        /// </summary>
        public ETLSource()
        {
            Title = "Source";
            HeaderColor = MaterialControl.MaterialColors.PrimaryContainer; // Material token
            Width = 140;
            Height = 72;
            DisplayText = "Source";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 0, outCount: 1);
        }

        /// <summary>
        /// Draws the cylinder shape representing a data source.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw cylinder body
            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(new SKRect(rect.Left, rect.Top + 8, rect.Right, rect.Bottom - 8), fill);

            // Draw top ellipse
            canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), fill);

            // Draw bottom ellipse
            canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), fill);

            // Draw cylinder outline
            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            canvas.DrawLine(rect.Left, rect.Top + 8, rect.Left, rect.Bottom - 8, border);
            canvas.DrawLine(rect.Right, rect.Top + 8, rect.Right, rect.Bottom - 8, border);
            canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), border);
            canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), border);
        }

        /// <summary>
        /// Gets the effective text positioning bounds for the cylinder shape.
        /// For cylinder shapes, we adjust positioning to account for the curved top and bottom.
        /// </summary>
        /// <returns>A rectangle defining the effective bounds for text positioning.</returns>
        protected override SKRect GetTextPositioningBounds()
        {
            // For cylinder, use bounds that account for the elliptical caps
            // The effective positioning area is slightly inset from the full bounds
            return new SKRect(X + 2, Y + 8, X + Width - 2, Y + Height - 8);
        }

        /// <summary>
        /// Positions connection points for the cylinder shape.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Position output at the right side of the cylinder body
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                float bodyCenterY = rect.Top + 8 + (rect.Bottom - 8 - (rect.Top + 8)) / 2;
                outputPoint.Center = new SKPoint(rect.Right + PortRadius + 2, bodyCenterY);
                outputPoint.Position = outputPoint.Center;
                float r = PortRadius;
                outputPoint.Bounds = new SKRect(outputPoint.Center.X - r, outputPoint.Center.Y - r,
                                              outputPoint.Center.X + r, outputPoint.Center.Y + r);
                outputPoint.Rect = outputPoint.Bounds;
                outputPoint.Index = 0;
                outputPoint.Component = this;
                outputPoint.IsAvailable = true;
            }
        }
    }
}