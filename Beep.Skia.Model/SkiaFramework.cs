using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Beep.Skia
{
    /// <summary>
    /// Represents the different states a component can be in during its lifecycle.
    /// </summary>
    public enum ComponentState
    {
        /// <summary>
        /// Component is being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// Component is active and ready for interaction.
        /// </summary>
        Active,

        /// <summary>
        /// Component is being updated.
        /// </summary>
        Updating,

        /// <summary>
        /// Component is being rendered.
        /// </summary>
        Rendering,

        /// <summary>
        /// Component is inactive.
        /// </summary>
        Inactive,

        /// <summary>
        /// Component is being disposed.
        /// </summary>
        Disposing
    }

    /// <summary>
    /// Defines the priority levels for component rendering and updates.
    /// </summary>
    public enum ComponentPriority
    {
        /// <summary>
        /// Lowest priority - rendered first.
        /// </summary>
        Background = 0,

        /// <summary>
        /// Normal priority - default level.
        /// </summary>
        Normal = 100,

        /// <summary>
        /// High priority - rendered after normal components.
        /// </summary>
        High = 200,

        /// <summary>
        /// Highest priority - rendered last (on top).
        /// </summary>
        Foreground = 300
    }

    /// <summary>
    /// Provides context information for drawing operations.
    /// </summary>
    public class DrawingContext
    {
        /// <summary>
        /// Gets or sets the current zoom level.
        /// </summary>
        public float Zoom { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the current pan offset.
        /// </summary>
        public SKPoint PanOffset { get; set; } = SKPoint.Empty;

        /// <summary>
        /// Gets or sets the current theme or style information.
        /// </summary>
        public object Theme { get; set; }

        /// <summary>
        /// Gets or sets the visible bounds for clipping and optimization.
        /// </summary>
        public SKRect Bounds { get; set; } = SKRect.Empty;

        /// <summary>
        /// Gets or sets additional context data.
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Provides context information for user interaction operations.
    /// </summary>
    public class InteractionContext
    {
        /// <summary>
        /// Gets or sets the current mouse position.
        /// </summary>
        public SKPoint MousePosition { get; set; }

        /// <summary>
        /// Gets or sets the mouse button that was pressed.
        /// </summary>
        public int MouseButton { get; set; }

        /// <summary>
        /// Gets or sets modifier keys that were pressed during the interaction.
        /// </summary>
        public int Modifiers { get; set; }

        /// <summary>
        /// Gets or sets the visible bounds for interaction checking.
        /// </summary>
        public SKRect Bounds { get; set; } = SKRect.Empty;

        /// <summary>
        /// Gets or sets additional context data.
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Generic interface for drawable components that can be rendered on a Skia canvas.
    /// This interface provides the basic contract for any visual component in the Beep.Skia platform.
    /// </summary>
    public interface IDrawableComponent
    {
        /// <summary>
        /// Gets or sets the X coordinate of this component's position.
        /// </summary>
        float X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of this component's position.
        /// </summary>
        float Y { get; set; }

        /// <summary>
        /// Gets or sets the width of this component.
        /// </summary>
        float Width { get; set; }

        /// <summary>
        /// Gets or sets the height of this component.
        /// </summary>
        float Height { get; set; }

        /// <summary>
        /// Gets or sets the name or identifier of this component.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets custom data associated with this component.
        /// </summary>
        object Tag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this component is visible.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this component is enabled for interaction.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Draws this component on the specified Skia canvas.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        /// <param name="context">The drawing context containing additional information.</param>
        void Draw(SKCanvas canvas, DrawingContext context);

        /// <summary>
        /// Determines whether the specified point is contained within this component.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>true if the point is contained within this component; otherwise, false.</returns>
        bool ContainsPoint(SKPoint point);

        /// <summary>
        /// Handles mouse down events for this component.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        bool HandleMouseDown(SKPoint point, InteractionContext context);

        /// <summary>
        /// Handles mouse move events for this component.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        bool HandleMouseMove(SKPoint point, InteractionContext context);

        /// <summary>
        /// Handles mouse up events for this component.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        bool HandleMouseUp(SKPoint point, InteractionContext context);

        /// <summary>
        /// Determines whether this component is connected to another component.
        /// </summary>
        /// <param name="other">The component to check connection with.</param>
        /// <returns>true if connected; otherwise, false.</returns>
        bool IsConnectedTo(IDrawableComponent other);
    }

    /// <summary>
    /// Provides event arguments for bounds change events.
    /// </summary>
    public class SKRectEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new bounds of the component.
        /// </summary>
        public SKRect Bounds { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SKRectEventArgs"/> class.
        /// </summary>
        /// <param name="bounds">The new bounds of the component.</param>
        public SKRectEventArgs(SKRect bounds)
        {
            Bounds = bounds;
        }
    }
}
