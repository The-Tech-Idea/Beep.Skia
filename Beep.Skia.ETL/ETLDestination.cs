using SkiaSharp;
using Beep.Skia.ETL;
using Beep.Skia.Components;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Destination component that represents data destinations in ETL workflows.
    /// Displays as a funnel shape indicating data output/storage.
    /// </summary>
    public class ETLDestination : ETLControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLDestination"/> class.
        /// </summary>
        public ETLDestination()
        {
            Title = "Destination";
            HeaderColor = MaterialControl.MaterialColors.TertiaryContainer; // Material token
            Width = 160;
            Height = 72;
            DisplayText = "Destination";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 1, outCount: 0);
        }

        /// <summary>
        /// Draws the funnel shape representing a data destination.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            // Create funnel shape (wider at top, narrower at bottom)
            path.MoveTo(rect.Left + 10, rect.Top);
            path.LineTo(rect.Right - 10, rect.Top);
            path.LineTo(rect.Right - 30, rect.Bottom);
            path.LineTo(rect.Left + 30, rect.Bottom);
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
        /// Positions connection points for the funnel shape.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Position input at the top center of the funnel
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
        }
    }
}