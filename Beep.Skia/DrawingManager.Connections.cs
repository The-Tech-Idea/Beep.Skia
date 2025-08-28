using SkiaSharp;
using System.Linq;

namespace Beep.Skia
{
    public partial class DrawingManager
    {
        /// <summary>
        /// Connects two workflow components and creates a visual connection line between them.
        /// </summary>
        /// <param name="component1">The first component to connect.</param>
        /// <param name="component2">The second component to connect.</param>
        /// <exception cref="ArgumentNullException">Thrown when either component is null.</exception>
        /// <exception cref="ArgumentException">Thrown when trying to connect a component to itself.</exception>
        public void ConnectComponents(SkiaComponent component1, SkiaComponent component2)
        {
            if (component1 == null)
                throw new ArgumentNullException(nameof(component1), "First component cannot be null.");
            if (component2 == null)
                throw new ArgumentNullException(nameof(component2), "Second component cannot be null.");
            if (component1 == component2)
                throw new ArgumentException("Cannot connect component to itself.");

            component1.ConnectTo(component2);
            component2.ConnectTo(component1);

            IConnectionPoint connectionPoint1 = component1.OutConnectionPoints.FirstOrDefault();
            IConnectionPoint connectionPoint2 = component2.InConnectionPoints.FirstOrDefault();
            if (connectionPoint1 != null && connectionPoint2 != null)
            {
                var line = new ConnectionLine(connectionPoint1, connectionPoint2, () => { /* InvalidateSurface callback */ });
                _lines.Add(line);
                _historyManager.ExecuteAction(new ConnectComponentsAction(this, component1, component2, line));
            }
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Disconnects two workflow components and removes their visual connection line.
        /// </summary>
        /// <param name="component1">The first component to disconnect.</param>
        /// <param name="component2">The second component to disconnect.</param>
        public void DisconnectComponents(SkiaComponent component1, SkiaComponent component2)
        {
            component1.DisconnectFrom(component2);
            component2.DisconnectFrom(component1);

            var lineToRemove = _lines.FirstOrDefault(line => (line.Start.Component == component1 && line.End.Component == component2) || (line.Start.Component == component2 && line.End.Component == component1));
            if (lineToRemove != null)
            {
                _lines.Remove(lineToRemove);
                _historyManager.ExecuteAction(new DisconnectComponentsAction(this, component1, component2, lineToRemove));
            }
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Moves a connection line from one connection point to another.
        /// </summary>
        /// <param name="line">The line to move.</param>
        /// <param name="newStartPoint">The new start connection point (optional).</param>
        /// <param name="newEndPoint">The new end connection point (optional).</param>
        public void MoveConnectionLine(IConnectionLine line, IConnectionPoint newStartPoint = null, IConnectionPoint newEndPoint = null)
        {
            if (line == null) return;

            var oldStartPoint = line.Start;
            var oldEndPoint = line.End;

            if (newStartPoint != null)
            {
                // Disconnect from old start point
                if (oldStartPoint != null)
                {
                    oldStartPoint.Connection = null;
                    oldStartPoint.IsAvailable = true;
                }

                // Connect to new start point
                line.Start = newStartPoint;
                newStartPoint.Connection = line.End;
                newStartPoint.IsAvailable = false;
            }

            if (newEndPoint != null)
            {
                // Disconnect from old end point
                if (oldEndPoint != null)
                {
                    oldEndPoint.Connection = null;
                    oldEndPoint.IsAvailable = true;
                }

                // Connect to new end point
                line.End = newEndPoint;
                newEndPoint.Connection = line.Start;
                newEndPoint.IsAvailable = false;
            }

            _historyManager.ExecuteAction(new MoveLineAction(this, line, oldStartPoint, oldEndPoint, newStartPoint, newEndPoint));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Adds a connection line to the drawing manager.
        /// </summary>
        /// <param name="line">The line to add.</param>
        internal void AddLine(IConnectionLine line)
        {
            if (line != null && !_lines.Contains(line))
            {
                _lines.Add(line);
            }
        }

        /// <summary>
        /// Removes a connection line from the drawing manager.
        /// </summary>
        /// <param name="line">The line to remove.</param>
        internal void RemoveLine(IConnectionLine line)
        {
            _lines.Remove(line);
        }

        /// <summary>
        /// Gets the connection point at the specified point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The connection point at the specified point, or null if no connection point is found.</returns>
        internal IConnectionPoint GetConnectionPointAt(SKPoint point)
        {
            foreach (var component in _components)
            {
                var connectionPoint = component.InConnectionPoints.Concat(component.OutConnectionPoints).FirstOrDefault(cp => cp.Bounds.Contains(point));
                if (connectionPoint != null)
                {
                    return connectionPoint;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the connection line at the specified point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The connection line at the specified point, or null if no line is found.</returns>
        internal IConnectionLine GetLineAt(SKPoint point)
        {
            return _lines.FirstOrDefault(line => line.LineContainsPoint(point));
        }

        /// <summary>
        /// Gets the arrow type ("start" or "end") at the specified point on a line.
        /// </summary>
        /// <param name="line">The connection line.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>"start", "end", or null if no arrow is at the point.</returns>
        internal string GetArrowAt(IConnectionLine line, SKPoint point)
        {
            return _renderingHelper.GetArrowAt(line, point);
        }

        /// <summary>
        /// Gets the resize handle at the specified point on a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The handle identifier, or null if no handle is at the point.</returns>
        internal string GetResizeHandleAt(SkiaComponent component, SKPoint point)
        {
            return _renderingHelper.GetResizeHandleAt(component, point);
        }

        /// <summary>
        /// Starts drawing a connection line from the specified source point.
        /// </summary>
        /// <param name="sourcePoint">The source connection point.</param>
        /// <param name="point">The current mouse position.</param>
        internal void StartDrawingLine(IConnectionPoint sourcePoint, SKPoint point)
        {
            var line = new ConnectionLine(sourcePoint, point, () => { /* InvalidateSurface callback */ });
            CurrentLine = line;
        }

        /// <summary>
        /// Gets or sets the current line being drawn (for internal use by helpers).
        /// </summary>
        internal IConnectionLine CurrentLine { get; set; }

        /// <summary>
        /// Gets a value indicating whether a line is currently being drawn.
        /// </summary>
        internal bool IsDrawingLine => CurrentLine != null;
    }
}
