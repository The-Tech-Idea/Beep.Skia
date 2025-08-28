using SkiaSharp;
using System;

namespace Beep.Skia
{
    /// <summary>
    /// Provides data for the ComponentDropped event.
    /// </summary>
    public class ComponentDropEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the component that was dropped.
        /// </summary>
        public SkiaComponent Component { get; }

        /// <summary>
        /// Gets the location where the component was dropped.
        /// </summary>
        public SKPoint Location { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentDropEventArgs"/> class.
        /// </summary>
        /// <param name="component">The component that was dropped.</param>
        /// <param name="location">The location where the component was dropped.</param>
        public ComponentDropEventArgs(SkiaComponent component, SKPoint location)
        {
            Component = component;
            Location = location;
        }
    }
}
