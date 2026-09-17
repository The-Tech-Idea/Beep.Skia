using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Legacy drop args, kept for source-compat only. Do not use.
    /// </summary>
    [Obsolete("Use Beep.Skia.Events.ComponentDropEventArgs (with Canvas/Screen positions) instead.", true)]
    internal class LegacyComponentDropEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public SkiaComponent Component { get; }
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public SKPoint Location { get; }

        /// <summary>
        /// Initializes a new instance of the LegacyComponentDropEventArgs class.
        /// </summary>
        public LegacyComponentDropEventArgs(SkiaComponent component, SKPoint location)
        {
            Component = component;
            Location = location;
        }
    }
}
