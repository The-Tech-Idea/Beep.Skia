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
        public SkiaComponent Component { get; init; }
        public SKPoint CanvasPosition { get; init; }
        public SKPoint ScreenPosition { get; init; }
        public SKRect Bounds { get; init; }
        // Back-compat single position field (canvas space)
        public SKPoint Location => CanvasPosition;
    }
}
