using System.Collections.Generic;
using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia.Layout
{
    /// <summary>
    /// Interface for layout managers that can arrange components within a container.
    /// </summary>
    public interface ILayoutManager
    {
        /// <summary>
        /// Lays out the specified components within the given bounds.
        /// </summary>
        /// <param name="components">The components to layout.</param>
        /// <param name="bounds">The bounds in which to layout the components.</param>
        /// <param name="padding">The padding around the layout area.</param>
        void Layout(IEnumerable<SkiaComponent> components, SKRect bounds, float padding = 0);
    }
}
