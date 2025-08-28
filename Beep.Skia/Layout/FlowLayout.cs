using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Layout
{
    /// <summary>
    /// A layout manager that arranges components in a flow, similar to text flow.
    /// Components are placed horizontally until there's no more space, then wrap to the next line.
    /// </summary>
    public class FlowLayout : ILayoutManager
    {
        /// <summary>
        /// Gets or sets the horizontal spacing between components.
        /// </summary>
        public float HorizontalSpacing { get; set; } = 5;

        /// <summary>
        /// Gets or sets the vertical spacing between rows of components.
        /// </summary>
        public float VerticalSpacing { get; set; } = 5;

        /// <summary>
        /// Lays out the specified components within the given bounds.
        /// </summary>
        /// <param name="components">The components to layout.</param>
        /// <param name="bounds">The bounds in which to layout the components.</param>
        /// <param name="padding">The padding around the layout area.</param>
        public void Layout(IEnumerable<SkiaComponent> components, SKRect bounds, float padding = 0)
        {
            var componentList = components.ToList();
            if (!componentList.Any())
                return;

            var layoutBounds = new SKRect(
                bounds.Left + padding,
                bounds.Top + padding,
                bounds.Right - padding,
                bounds.Bottom - padding
            );

            float currentX = layoutBounds.Left;
            float currentY = layoutBounds.Top;
            float maxHeightInRow = 0;

            foreach (var component in componentList)
            {
                // Check if component fits in current row
                if (currentX + component.Width > layoutBounds.Right && currentX > layoutBounds.Left)
                {
                    // Move to next row
                    currentX = layoutBounds.Left;
                    currentY += maxHeightInRow + VerticalSpacing;
                    maxHeightInRow = 0;
                }

                // Check if we have enough vertical space
                if (currentY + component.Height > layoutBounds.Bottom)
                {
                    // No more space, stop laying out
                    break;
                }

                // Position the component
                component.X = currentX;
                component.Y = currentY;

                // Update tracking variables
                currentX += component.Width + HorizontalSpacing;
                maxHeightInRow = Math.Max(maxHeightInRow, component.Height);
            }
        }
    }
}
