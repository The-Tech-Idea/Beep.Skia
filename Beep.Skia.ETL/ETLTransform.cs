using SkiaSharp;
using Beep.Skia.ETL;
using Beep.Skia.Components;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Transform component that represents data transformation operations in ETL workflows.
    /// Displays as a hexagon shape indicating data processing/transformation.
    /// </summary>
    public class ETLTransform : ETLControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETLTransform"/> class.
        /// </summary>
        public ETLTransform()
        {
            Title = "Transform";
            HeaderColor = MaterialControl.MaterialColors.SecondaryContainer; // Material token
            Width = 160;
            Height = 84;
            DisplayText = "Transform";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 1, outCount: 1);
        }

        /// <summary>
        /// Draws the hexagon shape representing a data transformation.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            using var path = new SKPath();
            float indent = 20f;
            // Create hexagon
            path.MoveTo(rect.Left + indent, rect.Top);
            path.LineTo(rect.Right - indent, rect.Top);
            path.LineTo(rect.Right, rect.MidY);
            path.LineTo(rect.Right - indent, rect.Bottom);
            path.LineTo(rect.Left + indent, rect.Bottom);
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
        /// Positions connection points at the hexagon's left and right points.
        /// </summary>
        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Position input at the left point of the hexagon
            if (InConnectionPoints.Count > 0)
            {
                var inputPoint = InConnectionPoints[0];
                inputPoint.Center = new SKPoint(rect.Left - PortRadius - 2, rect.MidY);
                inputPoint.Position = inputPoint.Center;
                float r = PortRadius;
                inputPoint.Bounds = new SKRect(inputPoint.Center.X - r, inputPoint.Center.Y - r,
                                             inputPoint.Center.X + r, inputPoint.Center.Y + r);
                inputPoint.Rect = inputPoint.Bounds;
                inputPoint.Index = 0;
                inputPoint.Component = this;
                inputPoint.IsAvailable = true;
            }

            // Position output at the right point of the hexagon
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                outputPoint.Center = new SKPoint(rect.Right + PortRadius + 2, rect.MidY);
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