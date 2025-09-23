using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Represents a connection point on a workflow component that can be connected to other connection points.
    /// This class implements the IConnectionPoint interface and provides functionality for managing
    /// connections between workflow components.
    /// </summary>
    public class ConnectionPoint : IConnectionPoint
    {
        /// <summary>
        /// Gets the unique identifier of this connection point.
        /// </summary>
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoint"/> class.
        /// </summary>
        public ConnectionPoint()
        {

        }

        /// <summary>
        /// Assigns a specific identifier to this connection point. Intended for restoring from persistence.
        /// </summary>
        /// <param name="id">The identifier to assign.</param>
        internal void SetId(Guid id)
        {
            if (id == Guid.Empty) return;
            Id = id;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this connection point is available for connection.
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Gets or sets the position of this connection point.
        /// </summary>
        public SKPoint Position { get; set; }

        /// <summary>
        /// Gets or sets the offset of this connection point from its parent component.
        /// </summary>
        public SKPoint Offset { get; set; }

        /// <summary>
        /// Gets or sets the center point of this connection point.
        /// </summary>
        public SKPoint Center { get; set; }

        /// <summary>
        /// Gets or sets the rectangular bounds of this connection point.
        /// </summary>
        public SKRect Rect { get; set; }

        /// <summary>
        /// Gets or sets the radius of this connection point (used for circular connection points).
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        /// Gets or sets the index of this connection point within its parent component.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the bounding rectangle of this connection point.
        /// </summary>
        public SKRect Bounds { get; set; }

        /// <summary>
        /// Gets or sets the shape of this connection point.
        /// </summary>
        public ComponentShape Shape { get; set; } = ComponentShape.Circle;

        /// <summary>
        /// Gets or sets the parent workflow component that owns this connection point.
        /// </summary>
        public IDrawableComponent Component { get; set; }

        /// <summary>
        /// Gets or sets the connection point that this connection point is connected to.
        /// </summary>
        public IConnectionPoint Connection { get; set; }

        /// <summary>
        /// Gets or sets the type of this connection point (input or output).
        /// </summary>
        public ConnectionPointType Type { get; set; }

        /// <summary>
        /// Gets or sets the data type of this connection point (e.g., "string", "number", "object").
        /// Used for data flow visualization and connection validation.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Occurs when a connection is made from this connection point to another connection point.
        /// </summary>
        public event EventHandler<ConnectionEventArgs> ConnectionMade;

        /// <summary>
        /// Connects this connection point to the specified target connection point.
        /// </summary>
        /// <param name="target">The target connection point to connect to.</param>
        public void ConnectTo(IConnectionPoint target)
        {
            ConnectionMade?.Invoke(this, new ConnectionEventArgs(this, target));
            Connection = target;
            this.IsAvailable = false;
            target.IsAvailable = false;
        }

        /// <summary>
        /// Disconnects this connection point from any connected connection point.
        /// </summary>
        public void Disconnect()
        {
            this.IsAvailable = true;
            if (Connection != null)
            {
                Connection.IsAvailable = true;
                Connection = null;
            }
        }
    }
}
