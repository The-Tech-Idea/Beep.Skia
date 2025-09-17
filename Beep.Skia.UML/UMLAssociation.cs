using SkiaSharp;
using Beep.Skia;
using System;

namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML association relationship between two UML elements.
    /// Can display multiplicity and role names.
    /// </summary>
    public class UMLAssociation : ConnectionLine
    {
        /// <summary>
        /// Gets or sets the multiplicity at the source end.
        /// </summary>
        public string SourceMultiplicity { get; set; } = "1";

        /// <summary>
        /// Gets or sets the multiplicity at the target end.
        /// </summary>
        public string TargetMultiplicity { get; set; } = "1";

        /// <summary>
        /// Gets or sets the role name at the source end.
        /// </summary>
        public string SourceRole { get; set; } = "";

        /// <summary>
        /// Gets or sets the role name at the target end.
        /// </summary>
        public string TargetRole { get; set; } = "";

        /// <summary>
        /// Gets or sets the type of association.
        /// </summary>
        public AssociationType AssociationType { get; set; } = AssociationType.Association;

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLAssociation"/> class.
        /// </summary>
        public UMLAssociation() : base(() => { })
        {
            // Set default styling through Paint
            if (Paint != null)
            {
                Paint.Color = SKColors.Black;
                Paint.StrokeWidth = 2;
            }
        }

        /// <summary>
        /// Draws the association line with UML-specific decorations.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public new void Draw(SKCanvas canvas)
        {
            // Call base Draw method first
            base.Draw(canvas);

            // Draw association-specific decorations
            DrawAssociationDecorations(canvas);
        }

        /// <summary>
        /// Draws UML-specific decorations for the association.
        /// </summary>
        private void DrawAssociationDecorations(SKCanvas canvas)
        {
            if (Start == null || End == null)
                return;

            using var font = new SKFont(SKTypeface.Default, 10);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            // Draw multiplicity and role names
            DrawEndLabels(canvas, font, paint);
        }

        /// <summary>
        /// Draws the multiplicity and role labels at both ends.
        /// </summary>
        private void DrawEndLabels(SKCanvas canvas, SKFont font, SKPaint paint)
        {
            const float labelOffset = 15;

            // Source end labels
            if (!string.IsNullOrEmpty(SourceMultiplicity) || !string.IsNullOrEmpty(SourceRole))
            {
                var sourcePos = CalculateLabelPosition(Start.Position, End.Position, false, labelOffset);
                DrawLabel(canvas, SourceMultiplicity, SourceRole, sourcePos, font, paint, false);
            }

            // Target end labels
            if (!string.IsNullOrEmpty(TargetMultiplicity) || !string.IsNullOrEmpty(TargetRole))
            {
                var targetPos = CalculateLabelPosition(Start.Position, End.Position, true, labelOffset);
                DrawLabel(canvas, TargetMultiplicity, TargetRole, targetPos, font, paint, true);
            }
        }

        /// <summary>
        /// Draws a label with multiplicity and role name.
        /// </summary>
        private void DrawLabel(SKCanvas canvas, string multiplicity, string role, SKPoint position,
                              SKFont font, SKPaint paint, bool isTargetEnd)
        {
            var lines = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(multiplicity))
                lines.Add(multiplicity);
            if (!string.IsNullOrEmpty(role))
                lines.Add(role);

            if (lines.Count == 0)
                return;

            float lineHeight = font.Size + 2;
            float totalHeight = lines.Count * lineHeight;
            float currentY = position.Y - totalHeight / 2 + lineHeight / 2;

            foreach (var line in lines)
            {
                var textWidth = font.MeasureText(line);
                float x = isTargetEnd ? position.X - textWidth - 5 : position.X + 5;
                canvas.DrawText(line, x, currentY, font, paint);
                currentY += lineHeight;
            }
        }

        /// <summary>
        /// Calculates the position for end labels.
        /// </summary>
        private SKPoint CalculateLabelPosition(SKPoint start, SKPoint end, bool isTargetEnd, float offset)
        {
            var direction = new SKPoint(end.X - start.X, end.Y - start.Y);
            var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0)
                return start;

            // Normalize direction
            direction = new SKPoint(direction.X / length, direction.Y / length);
            var perpendicular = new SKPoint(-direction.Y, direction.X);

            var basePoint = isTargetEnd ? end : start;
            return new SKPoint(
                basePoint.X + perpendicular.X * offset,
                basePoint.Y + perpendicular.Y * offset
            );
        }
    }

    /// <summary>
    /// Defines the types of UML associations.
    /// </summary>
    public enum AssociationType
    {
        /// <summary>
        /// Basic association relationship.
        /// </summary>
        Association,

        /// <summary>
        /// Aggregation relationship.
        /// </summary>
        Aggregation,

        /// <summary>
        /// Composition relationship.
        /// </summary>
        Composition,

        /// <summary>
        /// Inheritance relationship.
        /// </summary>
        Inheritance,

        /// <summary>
        /// Realization relationship.
        /// </summary>
        Realization
    }
}