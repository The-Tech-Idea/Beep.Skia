using SkiaSharp;
using Beep.Skia.ETL;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Filter component that represents data filtering operations in ETL workflows.
    /// Displays as a diamond shape indicating conditional data filtering.
    /// </summary>
    public class ETLFilter : ETLControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLFilter"/> class.
        /// </summary>
        public ETLFilter()
        {
            Title = "Filter";
            HeaderColor = new SKColor(0xFB, 0xBC, 0x05); // Yellow/Orange
            Width = 140;
            Height = 72;
            DisplayText = "Filter";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 1, outCount: 1);
        }

        /// <summary>
        /// Draws the diamond shape representing a data filter.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            // Create diamond
            path.MoveTo(rect.MidX, rect.Top);
            path.LineTo(rect.Right, rect.MidY);
            path.LineTo(rect.MidX, rect.Bottom);
            path.LineTo(rect.Left, rect.MidY);
            path.Close();

            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawPath(path, fill);

            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            canvas.DrawPath(path, border);
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

        /// <summary>
        /// Positions connection points at the diamond's corners for proper visual alignment.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Position input at top point of diamond
            if (InConnectionPoints.Count > 0)
            {
                var inputPoint = InConnectionPoints[0];
                inputPoint.Center = new SKPoint(rect.MidX, rect.Top - PortRadius - 2);
                inputPoint.Position = inputPoint.Center;
                float r = PortRadius;
                inputPoint.Bounds = new SKRect(inputPoint.Center.X - r, inputPoint.Center.Y - r,
                                             inputPoint.Center.X + r, inputPoint.Center.Y + r);
                inputPoint.Rect = inputPoint.Bounds;
                inputPoint.Index = 0;
                inputPoint.Component = this;
                inputPoint.IsAvailable = true;
            }

            // Position output at bottom point of diamond
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                outputPoint.Center = new SKPoint(rect.MidX, rect.Bottom + PortRadius + 2);
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