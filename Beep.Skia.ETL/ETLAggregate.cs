using SkiaSharp;
using Beep.Skia.ETL;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Aggregate component that represents data aggregation operations in ETL workflows.
    /// Displays as an octagon shape indicating data summarization/grouping.
    /// </summary>
    public class ETLAggregate : ETLControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLAggregate"/> class.
        /// </summary>
        public ETLAggregate()
        {
            Title = "Aggregate";
            HeaderColor = new SKColor(0x00, 0x95, 0x88); // Teal
            Width = 160;
            Height = 96;
            DisplayText = "Aggregate";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 1, outCount: 1);
        }

        /// <summary>
        /// Draws the octagon shape representing a data aggregation operation.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            float corner = 15f;
            // Create octagon (rectangle with cut corners)
            path.MoveTo(rect.Left + corner, rect.Top);
            path.LineTo(rect.Right - corner, rect.Top);
            path.LineTo(rect.Right, rect.Top + corner);
            path.LineTo(rect.Right, rect.Bottom - corner);
            path.LineTo(rect.Right - corner, rect.Bottom);
            path.LineTo(rect.Left + corner, rect.Bottom);
            path.LineTo(rect.Left, rect.Bottom - corner);
            path.LineTo(rect.Left, rect.Top + corner);
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
        /// Positions connection points at the octagon's left and right flat sides.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            float corner = 15f;

            // Position input at the middle of the left flat side
            if (InConnectionPoints.Count > 0)
            {
                var inputPoint = InConnectionPoints[0];
                float leftY = rect.Top + corner + (rect.Bottom - corner - (rect.Top + corner)) / 2;
                inputPoint.Center = new SKPoint(rect.Left - PortRadius - 2, leftY);
                inputPoint.Position = inputPoint.Center;
                float r = PortRadius;
                inputPoint.Bounds = new SKRect(inputPoint.Center.X - r, inputPoint.Center.Y - r,
                                             inputPoint.Center.X + r, inputPoint.Center.Y + r);
                inputPoint.Rect = inputPoint.Bounds;
                inputPoint.Index = 0;
                inputPoint.Component = this;
                inputPoint.IsAvailable = true;
            }

            // Position output at the middle of the right flat side
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                float rightY = rect.Top + corner + (rect.Bottom - corner - (rect.Top + corner)) / 2;
                outputPoint.Center = new SKPoint(rect.Right + PortRadius + 2, rightY);
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