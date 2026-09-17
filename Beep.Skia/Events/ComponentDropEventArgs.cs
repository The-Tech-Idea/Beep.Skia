using System;
using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>
    /// Event args for when a component is dropped after a drag operation.
    /// Provides final positions in canvas and screen space along with final bounds.
    /// </summary>
    public sealed class ComponentDropEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public SkiaComponent Component { get; init; }
        /// <summary>
        /// Gets or sets the canvas position.
        /// </summary>
        public SKPoint CanvasPosition { get; init; }
        /// <summary>
        /// Gets or sets the screen position.
        /// </summary>
        public SKPoint ScreenPosition { get; init; }
        /// <summary>
        /// Gets or sets the bounds.
        /// </summary>
        public SKRect Bounds { get; init; }
        // Back-compat single position field (canvas space)
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public SKPoint Location => CanvasPosition;
    }
}
