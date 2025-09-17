using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia.Layout
{
    /// <summary>
    /// Specifies the orientation of a stack layout.
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// Components are stacked vertically.
        /// </summary>
        Vertical,

        /// <summary>
        /// Components are stacked horizontally.
        /// </summary>
        Horizontal
    }

    /// <summary>
    /// A layout manager that arranges components in a stack, either vertically or horizontally.
    /// </summary>
    public class StackLayout : ILayoutManager
    {
        /// <summary>
        /// Gets or sets the orientation of the stack.
        /// </summary>
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets or sets the spacing between components.
        /// </summary>
        public float Spacing { get; set; } = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="StackLayout"/> class.
        /// </summary>
        public StackLayout() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackLayout"/> class with specified orientation.
        /// </summary>
        /// <param name="orientation">The orientation of the stack.</param>
        public StackLayout(Orientation orientation)
        {
            Orientation = orientation;
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
            if (!componentList.Any())
                return;

            var layoutBounds = new SKRect(
                bounds.Left + padding,
                bounds.Top + padding,
                bounds.Right - padding,
                bounds.Bottom - padding
            );

            if (Orientation == Orientation.Vertical)
            {
                LayoutVertical(componentList, layoutBounds);
            }
            else
            {
                LayoutHorizontal(componentList, layoutBounds);
            }
        }

        private void LayoutVertical(List<SkiaComponent> components, SKRect bounds)
        {
            float currentY = bounds.Top;

            foreach (var component in components)
            {
                if (currentY + component.Height > bounds.Bottom)
                    break;

                component.X = bounds.Left;
                component.Y = currentY;

                // Center horizontally if component is narrower than available space
                if (component.Width < bounds.Width)
                {
                    component.X = bounds.Left + (bounds.Width - component.Width) / 2;
                }

                currentY += component.Height + Spacing;
            }
        }

        private void LayoutHorizontal(List<SkiaComponent> components, SKRect bounds)
        {
            float currentX = bounds.Left;

            foreach (var component in components)
            {
                if (currentX + component.Width > bounds.Right)
                    break;

                component.X = currentX;
                component.Y = bounds.Top;

                // Center vertically if component is shorter than available space
                if (component.Height < bounds.Height)
                {
                    component.Y = bounds.Top + (bounds.Height - component.Height) / 2;
                }

                currentX += component.Width + Spacing;
            }
        }
    }
}
