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
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public IDrawableComponent Component { get; set; }
        /// <summary>
        /// Gets or sets the click position.
        /// </summary>
        public SKPoint ClickPosition { get; set; }
        /// <summary>
        /// Gets or sets the button.
        /// </summary>
        public MouseButton Button { get; set; }
        /// <summary>
        /// Gets or sets the is double click.
        /// </summary>
        public bool IsDoubleClick { get; set; }
    }

    /// <summary>
    /// Event args for connection line click events.
    /// </summary>
    public class LineClickEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public IConnectionLine Line { get; set; }
        /// <summary>
        /// Gets or sets the click position.
        /// </summary>
        public SKPoint ClickPosition { get; set; }
        /// <summary>
        /// Gets or sets the button.
        /// </summary>
        public MouseButton Button { get; set; }
        /// <summary>
        /// Gets or sets the is double click.
        /// </summary>
        public bool IsDoubleClick { get; set; }
    }

    /// <summary>
    /// Event args for diagram canvas click events (clicking empty space).
    /// </summary>
    public class DiagramClickEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the click position.
        /// </summary>
        public SKPoint ClickPosition { get; set; }
        /// <summary>
        /// Gets or sets the button.
        /// </summary>
        public MouseButton Button { get; set; }
    }

    /// <summary>
    /// Event args for hover state changes.
    /// </summary>
    public class HoverChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public IDrawableComponent Component { get; set; }
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public IConnectionLine Line { get; set; }
        /// <summary>
        /// Gets or sets the is hovered.
        /// </summary>
        public bool IsHovered { get; set; }
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        public SKPoint Position { get; set; }
    }
}
