using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Provides event arguments for connection events between connection points.
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the source connection point of the connection.
        /// </summary>
        public IConnectionPoint SourceIConnectionPoint { get; private set; }

        /// <summary>
        /// Gets the target connection point of the connection.
        /// </summary>
        public IConnectionPoint TargetIConnectionPoint { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionEventArgs"/> class.
        /// </summary>
        /// <param name="sourceIConnectionPoint">The source connection point.</param>
        /// <param name="targetIConnectionPoint">The target connection point.</param>
        public ConnectionEventArgs(IConnectionPoint sourceIConnectionPoint, IConnectionPoint targetIConnectionPoint)
        {
            SourceIConnectionPoint = sourceIConnectionPoint;
            TargetIConnectionPoint = targetIConnectionPoint;
        }
    }

    /// <summary>
    /// Provides event arguments for connection-related events involving a single connection point.
    /// </summary>
    public class ConnectionArgs : EventArgs
    {
        /// <summary>
        /// Gets the connection point involved in the event.
        /// </summary>
        public IConnectionPoint IConnectionPoint { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionArgs"/> class.
        /// </summary>
        /// <param name="iConnectionPoint">The connection point involved in the event.</param>
        public ConnectionArgs(IConnectionPoint iConnectionPoint)
        {
            IConnectionPoint = iConnectionPoint;
        }
    }

    /// <summary>
    /// Provides event arguments for line-related events involving connection points and lines.
    /// </summary>
    public class LineArgs : EventArgs
    {
        /// <summary>
        /// Gets the source connection point of the line.
        /// </summary>
        public IConnectionPoint SourceIConnectionPoint { get; private set; }

        /// <summary>
        /// Gets the target connection point of the line.
        /// </summary>
        public IConnectionPoint TargetIConnectionPoint { get; private set; }

        /// <summary>
        /// Gets the connection line associated with the event.
        /// </summary>
        public IConnectionLine ConnectionLine { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineArgs"/> class.
        /// </summary>
        /// <param name="sourceIConnectionPoint">The source connection point.</param>
        /// <param name="targetIConnectionPoint">The target connection point.</param>
        /// <param name="connectionLine">The connection line.</param>
        public LineArgs(IConnectionPoint sourceIConnectionPoint, IConnectionPoint targetIConnectionPoint, IConnectionLine connectionLine)
        {
            SourceIConnectionPoint = sourceIConnectionPoint;
            TargetIConnectionPoint = targetIConnectionPoint;
            ConnectionLine = connectionLine;
        }
    }
}
