using SkiaSharp;
using Beep.Skia.ETL;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Join component that represents data joining operations in ETL workflows.
    /// Displays as a triangle shape indicating data merging/combining.
    /// </summary>
    public class ETLJoin : ETLControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLJoin"/> class.
        /// </summary>
        public ETLJoin()
        {
            Title = "Join";
            HeaderColor = new SKColor(0x8E, 0x24, 0xAA); // Purple
            Width = 180;
            Height = 96;
            DisplayText = "Join";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 2, outCount: 1);
        }

        /// <summary>
        /// Draws the triangle shape representing a data join operation.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            // Create right-pointing triangle with flat left side
            path.MoveTo(rect.Left, rect.Top + 10);
            path.LineTo(rect.Right - 20, rect.MidY);
            path.LineTo(rect.Left, rect.Bottom - 10);
            path.LineTo(rect.Left + 20, rect.Bottom - 10);
            path.LineTo(rect.Left + 20, rect.Top + 10);
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
        /// Positions connection points appropriately for the triangle shape.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Position inputs along the left flat side of the triangle
            float leftX = rect.Left + 10; // Slightly inset from the edge
            float inputAreaTop = rect.Top + 20;
            float inputAreaBottom = rect.Bottom - 20;

            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                var point = InConnectionPoints[i];
                float t = InConnectionPoints.Count > 1 ? (i + 1) / (float)(InConnectionPoints.Count + 1) : 0.5f;
                float cy = inputAreaTop + t * (inputAreaBottom - inputAreaTop);

                point.Center = new SKPoint(leftX - PortRadius - 2, cy);
                point.Position = point.Center;
                float r = PortRadius;
                point.Bounds = new SKRect(point.Center.X - r, point.Center.Y - r,
                                        point.Center.X + r, point.Center.Y + r);
                point.Rect = point.Bounds;
                point.Index = i;
                point.Component = this;
                point.IsAvailable = true;
            }

            // Position output at the right point of the triangle
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                outputPoint.Center = new SKPoint(rect.Right - 20 + PortRadius + 2, rect.MidY);
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