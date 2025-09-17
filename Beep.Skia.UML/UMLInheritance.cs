using SkiaSharp;
using Beep.Skia;

namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML inheritance relationship between classes.
    /// Displays a solid line with a triangle arrowhead pointing to the parent class.
    /// </summary>
    public class UMLInheritance : ConnectionLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UMLInheritance"/> class.
        /// </summary>
        public UMLInheritance() : base(() => { })
        {
            // Set default styling for inheritance
            if (Paint != null)
            {
                Paint.Color = SKColors.Black;
                Paint.StrokeWidth = 2;
            }

            // Disable default arrows since we draw our own triangle
            ShowStartArrow = false;
            ShowEndArrow = false;
        }

        /// <summary>
        /// Draws the inheritance relationship with a triangle arrowhead.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public new void Draw(SKCanvas canvas)
        {
            // Draw the basic line
            base.Draw(canvas);

            // Draw inheritance-specific triangle decoration
            DrawInheritanceTriangle(canvas);
        }

        /// <summary>
        /// Draws the triangle arrowhead for inheritance.
        /// </summary>
        private void DrawInheritanceTriangle(SKCanvas canvas)
        {
            if (Start == null || End == null)
                return;

            var startPoint = Start.Position;
            var endPoint = End.Position;

            // Calculate direction from child to parent
            var direction = new SKPoint(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
            var length = (float)System.Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0)
                return;

            // Normalize direction
            direction = new SKPoint(direction.X / length, direction.Y / length);
            var perpendicular = new SKPoint(-direction.Y, direction.X);

            const float triangleSize = 12;

            // Position triangle at the end point (parent class)
            var tip = endPoint;
            var left = new SKPoint(
                endPoint.X - direction.X * triangleSize + perpendicular.X * triangleSize / 2,
                endPoint.Y - direction.Y * triangleSize + perpendicular.Y * triangleSize / 2
            );
            var right = new SKPoint(
                endPoint.X - direction.X * triangleSize - perpendicular.X * triangleSize / 2,
                endPoint.Y - direction.Y * triangleSize - perpendicular.Y * triangleSize / 2
            );

            // Draw the triangle
            using var paint = new SKPaint
            {
                Color = Paint?.Color ?? SKColors.Black,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var path = new SKPath();
            path.MoveTo(tip);
            path.LineTo(left);
            path.LineTo(right);
            path.Close();

            canvas.DrawPath(path, paint);
        }
    }
}