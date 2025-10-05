using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Skia
{
    /// <summary>
    /// Event args for component (node) click events.
    /// </summary>
    public class ComponentClickEventArgs : EventArgs
    {
        public IDrawableComponent Component { get; set; }
        public SKPoint ClickPosition { get; set; }
        public MouseButton Button { get; set; }
        public bool IsDoubleClick { get; set; }
    }

    /// <summary>
    /// Event args for connection line click events.
    /// </summary>
    public class LineClickEventArgs : EventArgs
    {
        public IConnectionLine Line { get; set; }
        public SKPoint ClickPosition { get; set; }
        public MouseButton Button { get; set; }
        public bool IsDoubleClick { get; set; }
    }

    /// <summary>
    /// Event args for diagram canvas click events (clicking empty space).
    /// </summary>
    public class DiagramClickEventArgs : EventArgs
    {
        public SKPoint ClickPosition { get; set; }
        public MouseButton Button { get; set; }
    }

    /// <summary>
    /// Event args for hover state changes.
    /// </summary>
    public class HoverChangedEventArgs : EventArgs
    {
        public IDrawableComponent Component { get; set; }
        public IConnectionLine Line { get; set; }
        public bool IsHovered { get; set; }
        public SKPoint Position { get; set; }
    }
}
