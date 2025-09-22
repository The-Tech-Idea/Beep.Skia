using SkiaSharp;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents a connection point on a workflow component that can be used to create connections between components.
    /// Connection points can be either input or output points and support different shapes.
    /// </summary>
    public interface IConnectionPoint
    {
        /// <summary>
        /// Gets or sets the bounding rectangle of the connection point.
        /// </summary>
        SKRect Bounds { get; set; }

        /// <summary>
        /// Gets or sets the center point of the connection point.
        /// </summary>
        SKPoint Center { get; set; }

        /// <summary>
        /// Gets or sets the component that owns this connection point.
        /// </summary>
        IDrawableComponent Component { get; set; }

        /// <summary>
        /// Gets or sets the connection point that this point is connected to.
        /// </summary>
        IConnectionPoint Connection { get; set; }

        /// <summary>
        /// Gets or sets the index of this connection point within its component.
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection point is available for connection.
        /// </summary>
        bool IsAvailable { get; set; }

        /// <summary>
        /// Gets or sets the offset of this connection point from its component's origin.
        /// </summary>
        SKPoint Offset { get; set; }

        /// <summary>
        /// Gets or sets the position of this connection point in canvas coordinates.
        /// </summary>
        SKPoint Position { get; set; }

        /// <summary>
        /// Gets or sets the radius of this connection point for hit testing and drawing.
        /// </summary>
        int Radius { get; set; }

        /// <summary>
        /// Gets or sets the rectangle that defines the connection point's area.
        /// </summary>
        SKRect Rect { get; set; }

        /// <summary>
        /// Gets or sets the shape of this connection point.
        /// </summary>
        ComponentShape Shape { get; set; }

        /// <summary>
        /// Gets or sets the type of this connection point (input or output).
        /// </summary>
        ConnectionPointType Type { get; set; }

        /// <summary>
        /// Gets or sets the data type of this connection point (e.g., "string", "number", "object").
        /// Used for data flow visualization and connection validation.
        /// </summary>
        string DataType { get; set; }

        /// <summary>
        /// Event raised when a connection is made to this connection point.
        /// </summary>
        event EventHandler<ConnectionEventArgs> ConnectionMade;

        /// <summary>
        /// Connects this connection point to the specified target connection point.
        /// </summary>
        /// <param name="target">The target connection point to connect to.</param>
        void ConnectTo(IConnectionPoint target);

        /// <summary>
        /// Disconnects this connection point from any connected point.
        /// </summary>
        void Disconnect();
    }
}