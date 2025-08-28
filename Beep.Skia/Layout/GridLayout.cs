using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Layout
{
    /// <summary>
    /// A layout manager that arranges components in a grid with a fixed number of columns.
    /// </summary>
    public class GridLayout : ILayoutManager
    {
        /// <summary>
        /// Gets or sets the number of columns in the grid.
        /// </summary>
        public int Columns { get; set; } = 1;

        /// <summary>
        /// Gets or sets the horizontal spacing between components.
        /// </summary>
        public float HorizontalSpacing { get; set; } = 5;

        /// <summary>
        /// Gets or sets the vertical spacing between components.
        /// </summary>
        public float VerticalSpacing { get; set; } = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLayout"/> class.
        /// </summary>
        public GridLayout() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLayout"/> class with specified columns.
        /// </summary>
        /// <param name="columns">The number of columns in the grid.</param>
        public GridLayout(int columns)
        {
            Columns = Math.Max(1, columns);
        }

        /// <summary>
        /// Lays out the specified components within the given bounds.
        /// </summary>
        /// <param name="components">The components to layout.</param>
        /// <param name="bounds">The bounds in which to layout the components.</param>
        /// <param name="padding">The padding around the layout area.</param>
        public void Layout(IEnumerable<SkiaComponent> components, SKRect bounds, float padding = 0)
        {
            var componentList = components.ToList();
            if (!componentList.Any() || Columns <= 0)
                return;

            var layoutBounds = new SKRect(
                bounds.Left + padding,
                bounds.Top + padding,
                bounds.Right - padding,
                bounds.Bottom - padding
            );

            // Calculate cell dimensions
            float cellWidth = (layoutBounds.Width - (Columns - 1) * HorizontalSpacing) / Columns;
            int rows = (int)Math.Ceiling((double)componentList.Count / Columns);

            for (int i = 0; i < componentList.Count; i++)
            {
                int row = i / Columns;
                int col = i % Columns;

                float x = layoutBounds.Left + col * (cellWidth + HorizontalSpacing);
                float y = layoutBounds.Top + row * (componentList[i].Height + VerticalSpacing);

                // Check if component fits vertically
                if (y + componentList[i].Height > layoutBounds.Bottom)
                    break;

                componentList[i].X = x;
                componentList[i].Y = y;

                // Resize component to fit cell width if it's larger
                if (componentList[i].Width > cellWidth)
                {
                    componentList[i].Width = cellWidth;
                }
            }
        }
    }
}
