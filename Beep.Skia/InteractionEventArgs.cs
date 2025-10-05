using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Skia
{
    /// <summary>
    /// Event arguments for component interaction events.
    /// Provides detailed information about component clicks, hovers, and other interactions.
    /// </summary>
    public class ComponentInteractionEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the component that was interacted with.
        /// </summary>
        public SkiaComponent Component { get; }

        /// <summary>
        /// Gets the mouse position in canvas coordinates where the interaction occurred.
        /// </summary>
        public SKPoint CanvasPosition { get; }

        /// <summary>
        /// Gets the mouse position in screen coordinates where the interaction occurred.
        /// </summary>
        public SKPoint ScreenPosition { get; }

        /// <summary>
        /// Gets the mouse button involved in the interaction (0=left, 1=right, 2=middle).
        /// </summary>
        public int MouseButton { get; }

        /// <summary>
        /// Gets the keyboard modifiers pressed during the interaction.
        /// </summary>
        public SKKeyModifiers Modifiers { get; }

        /// <summary>
        /// Gets or sets whether this event has been handled and should not propagate further.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the interaction type (Click, DoubleClick, RightClick, Hover, etc.).
        /// </summary>
        public InteractionType InteractionType { get; }

        /// <summary>
        /// Gets custom data associated with this interaction.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Initializes a new instance of the ComponentInteractionEventArgs class.
        /// </summary>
        public ComponentInteractionEventArgs(
            SkiaComponent component,
            SKPoint canvasPosition,
            SKPoint screenPosition,
            int mouseButton,
            SKKeyModifiers modifiers,
            InteractionType interactionType)
        {
            Component = component;
            CanvasPosition = canvasPosition;
            ScreenPosition = screenPosition;
            MouseButton = mouseButton;
            Modifiers = modifiers;
            InteractionType = interactionType;
            Handled = false;
        }
    }

    /// <summary>
    /// Event arguments for connection line interaction events.
    /// </summary>
    public class LineInteractionEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the connection line that was interacted with.
        /// </summary>
        public IConnectionLine Line { get; }

        /// <summary>
        /// Gets the mouse position in canvas coordinates where the interaction occurred.
        /// </summary>
        public SKPoint CanvasPosition { get; }

        /// <summary>
        /// Gets the mouse position in screen coordinates where the interaction occurred.
        /// </summary>
        public SKPoint ScreenPosition { get; }

        /// <summary>
        /// Gets the mouse button involved in the interaction (0=left, 1=right, 2=middle).
        /// </summary>
        public int MouseButton { get; }

        /// <summary>
        /// Gets the keyboard modifiers pressed during the interaction.
        /// </summary>
        public SKKeyModifiers Modifiers { get; }

        /// <summary>
        /// Gets or sets whether this event has been handled and should not propagate further.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the interaction type (Click, DoubleClick, RightClick, Hover, etc.).
        /// </summary>
        public InteractionType InteractionType { get; }

        /// <summary>
        /// Gets whether an arrow head was clicked (true) or the line itself (false).
        /// </summary>
        public bool IsArrowClick { get; }

        /// <summary>
        /// Gets custom data associated with this interaction.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Initializes a new instance of the LineInteractionEventArgs class.
        /// </summary>
        public LineInteractionEventArgs(
            IConnectionLine line,
            SKPoint canvasPosition,
            SKPoint screenPosition,
            int mouseButton,
            SKKeyModifiers modifiers,
            InteractionType interactionType,
            bool isArrowClick = false)
        {
            Line = line;
            CanvasPosition = canvasPosition;
            ScreenPosition = screenPosition;
            MouseButton = mouseButton;
            Modifiers = modifiers;
            InteractionType = interactionType;
            IsArrowClick = isArrowClick;
            Handled = false;
        }
    }

    /// <summary>
    /// Event arguments for canvas/diagram interaction events.
    /// </summary>
    public class DiagramInteractionEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the mouse position in canvas coordinates where the interaction occurred.
        /// </summary>
        public SKPoint CanvasPosition { get; }

        /// <summary>
        /// Gets the mouse position in screen coordinates where the interaction occurred.
        /// </summary>
        public SKPoint ScreenPosition { get; }

        /// <summary>
        /// Gets the mouse button involved in the interaction (0=left, 1=right, 2=middle).
        /// </summary>
        public int MouseButton { get; }

        /// <summary>
        /// Gets the keyboard modifiers pressed during the interaction.
        /// </summary>
        public SKKeyModifiers Modifiers { get; }

        /// <summary>
        /// Gets or sets whether this event has been handled and should not propagate further.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the interaction type (Click, DoubleClick, RightClick, etc.).
        /// </summary>
        public InteractionType InteractionType { get; }

        /// <summary>
        /// Gets custom data associated with this interaction.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Initializes a new instance of the DiagramInteractionEventArgs class.
        /// </summary>
        public DiagramInteractionEventArgs(
            SKPoint canvasPosition,
            SKPoint screenPosition,
            int mouseButton,
            SKKeyModifiers modifiers,
            InteractionType interactionType)
        {
            CanvasPosition = canvasPosition;
            ScreenPosition = screenPosition;
            MouseButton = mouseButton;
            Modifiers = modifiers;
            InteractionType = interactionType;
            Handled = false;
        }
    }

    /// <summary>
    /// Types of interactions that can occur in the diagram.
    /// </summary>
    public enum InteractionType
    {
        /// <summary>
        /// Single left click.
        /// </summary>
        Click,

        /// <summary>
        /// Double click.
        /// </summary>
        DoubleClick,

        /// <summary>
        /// Right click (context menu).
        /// </summary>
        RightClick,

        /// <summary>
        /// Middle mouse button click.
        /// </summary>
        MiddleClick,

        /// <summary>
        /// Mouse entered the bounds (hover started).
        /// </summary>
        HoverEnter,

        /// <summary>
        /// Mouse left the bounds (hover ended).
        /// </summary>
        HoverLeave,

        /// <summary>
        /// Mouse is hovering (continuous).
        /// </summary>
        Hover,

        /// <summary>
        /// Drag started.
        /// </summary>
        DragStart,

        /// <summary>
        /// Dragging in progress.
        /// </summary>
        Dragging,

        /// <summary>
        /// Drag ended.
        /// </summary>
        DragEnd
    }
}
