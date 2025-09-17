using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;
namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents a UML diagram canvas that contains and manages multiple UML elements.
    /// Provides layout management, grid display, and diagram-level operations.
    /// </summary>
    public abstract class UMLDiagram : SkiaComponent
    {
        /// <summary>
        /// Gets the collection of UML elements in this diagram.
        /// </summary>
        public List<UMLControl> UMLElements { get; } = new List<UMLControl>();

        /// <summary>
        /// Gets the collection of connection lines in this diagram.
        /// </summary>
        public List<ConnectionLine> Connections { get; } = new List<ConnectionLine>();

        /// <summary>
        /// Gets or sets whether to show a grid background.
        /// </summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>
        /// Gets or sets the grid spacing in pixels.
        /// </summary>
        public float GridSpacing { get; set; } = 20;

        /// <summary>
        /// Gets or sets the grid color.
        /// </summary>
        public SKColor GridColor { get; set; } = new SKColor(200, 200, 200, 100);

        /// <summary>
        /// Gets or sets the diagram background color.
        /// </summary>
        public SKColor DiagramBackgroundColor { get; set; } = SKColors.White;

        /// <summary>
        /// Gets or sets the type of diagram.
        /// </summary>
        public DiagramType DiagramType { get; set; } = DiagramType.Class;

        /// <summary>
        /// Initializes a new instance of the <see cref="UMLDiagram"/> class.
        /// </summary>
        public UMLDiagram()
        {
            Name = "UMLDiagram";
            Width = 800;
            Height = 600;
        }

        /// <summary>
        /// Draws the diagram content including background, grid, and all elements.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw diagram background
            DrawBackground(canvas);

            // Draw grid if enabled
            if (ShowGrid)
            {
                DrawGrid(canvas);
            }

            // Draw all UML elements
            foreach (var element in UMLElements)
            {
                element.Draw(canvas, context);
            }

            // Draw all connections
            foreach (var connection in Connections)
            {
                connection.Draw(canvas);
            }
        }

        /// <summary>
        /// Public method to draw the diagram content (accessible from UMLEditor).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        public new void Draw(SKCanvas canvas, DrawingContext context)
        {
            DrawContent(canvas, context);
        }

        /// <summary>
        /// Draws the diagram background.
        /// </summary>
        private void DrawBackground(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = DiagramBackgroundColor,
                Style = SKPaintStyle.Fill
            };

            canvas.DrawRect(X, Y, Width, Height, paint);
        }

        /// <summary>
        /// Draws the grid background.
        /// </summary>
        private void DrawGrid(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = GridColor,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            };

            // Draw vertical lines
            for (float x = X; x <= X + Width; x += GridSpacing)
            {
                canvas.DrawLine(x, Y, x, Y + Height, paint);
            }

            // Draw horizontal lines
            for (float y = Y; y <= Y + Height; y += GridSpacing)
            {
                canvas.DrawLine(X, y, X + Width, y, paint);
            }
        }

        /// <summary>
        /// Adds a UML element to the diagram.
        /// </summary>
        /// <param name="element">The UML element to add.</param>
        public void AddElement(UMLControl element)
        {
            if (element != null && !UMLElements.Contains(element))
            {
                UMLElements.Add(element);
            }
        }

        /// <summary>
        /// Removes a UML element from the diagram.
        /// </summary>
        /// <param name="element">The UML element to remove.</param>
        public void RemoveElement(UMLControl element)
        {
            if (element != null && UMLElements.Remove(element))
            {
            }
        }

        /// <summary>
        /// Adds a connection line to the diagram.
        /// </summary>
        /// <param name="connection">The connection line to add.</param>
        public void AddConnection(ConnectionLine connection)
        {
            if (connection != null && !Connections.Contains(connection))
            {
                Connections.Add(connection);
            }
        }

        /// <summary>
        /// Removes a connection line from the diagram.
        /// </summary>
        /// <param name="connection">The connection line to remove.</param>
        public void RemoveConnection(ConnectionLine connection)
        {
            if (connection != null && Connections.Remove(connection))
            {
            }
        }

        /// <summary>
        /// Gets all UML elements at the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>A collection of UML elements at the point.</returns>
        public IEnumerable<UMLControl> GetElementsAtPoint(SKPoint point)
        {
            return UMLElements.Where(element => element.Bounds.Contains(point.X, point.Y));
        }

        /// <summary>
        /// Gets all connection lines at the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>A collection of connection lines at the point.</returns>
        public IEnumerable<ConnectionLine> GetConnectionsAtPoint(SKPoint point)
        {
            return Connections.Where(connection => IsPointOnLine(point, connection));
        }

        /// <summary>
        /// Checks if a point is on a connection line.
        /// </summary>
        private bool IsPointOnLine(SKPoint point, ConnectionLine connection)
        {
            if (connection.Start == null || connection.End == null)
                return false;

            var start = connection.Start.Position;
            var end = connection.End.Position;

            // Calculate distance from point to line
            var lineLength = SKPoint.Distance(start, end);
            if (lineLength == 0)
                return SKPoint.Distance(point, start) < 5;

            var t = System.Math.Max(0, System.Math.Min(1,
                ((point.X - start.X) * (end.X - start.X) + (point.Y - start.Y) * (end.Y - start.Y)) / (lineLength * lineLength)));

            var closest = new SKPoint(
                start.X + t * (end.X - start.X),
                start.Y + t * (end.Y - start.Y)
            );

            return SKPoint.Distance(point, closest) < 5;
        }

        /// <summary>
        /// Clears all elements and connections from the diagram.
        /// </summary>
        public void Clear()
        {
            UMLElements.Clear();
            Connections.Clear();
        }

        /// <summary>
        /// Gets the bounds of all elements in the diagram.
        /// </summary>
        /// <returns>The combined bounds of all elements.</returns>
        public SKRect GetContentBounds()
        {
            if (!UMLElements.Any() && !Connections.Any())
            {
                return new SKRect(X, Y, X + Width, Y + Height);
            }

            var bounds = UMLElements.First().Bounds;
            foreach (var element in UMLElements.Skip(1))
            {
                bounds = SKRect.Union(bounds, element.Bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Fits the diagram to show all content.
        /// </summary>
        public void FitToContent()
        {
            var contentBounds = GetContentBounds();
            if (contentBounds.IsEmpty)
                return;

            // Add some padding
            const float padding = 50;
            contentBounds = new SKRect(
                contentBounds.Left - padding,
                contentBounds.Top - padding,
                contentBounds.Right + padding,
                contentBounds.Bottom + padding
            );

            // Update diagram bounds
            X = contentBounds.Left;
            Y = contentBounds.Top;
            Width = contentBounds.Width;
            Height = contentBounds.Height;
        }

        /// <summary>
        /// Handles mouse events for the diagram.
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            // Check if we clicked on any UML elements
            var element = UMLElements.LastOrDefault(e => e.Bounds.Contains(point.X, point.Y));
            if (element != null)
            {
                // Element was clicked - mark as handled
                return true;
            }

            // Check connections
            var connection = Connections.LastOrDefault(c => IsPointOnLine(point, c));
            if (connection != null)
            {
                // Connection was clicked - mark as handled
                return true;
            }

            // Handle diagram background interaction
            return base.OnMouseDown(point, context);
        }

        /// <summary>
        /// Public method to handle mouse down events (accessible from UMLEditor).
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        public new bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            return OnMouseDown(point, context);
        }

        /// <summary>
        /// Handles mouse move events for the diagram.
        /// </summary>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            // Check if we're over any UML elements
            var element = UMLElements.LastOrDefault(e => e.Bounds.Contains(point.X, point.Y));
            if (element != null)
            {
                // Over an element - mark as handled
                return true;
            }

            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse up events for the diagram.
        /// </summary>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            // Check if we released over any UML elements
            var element = UMLElements.LastOrDefault(e => e.Bounds.Contains(point.X, point.Y));
            if (element != null)
            {
                // Released over an element - mark as handled
                return true;
            }

            return base.OnMouseUp(point, context);
        }

        /// <summary>
        /// Public method to handle mouse move events from external callers.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        public bool ProcessMouseMove(SKPoint point, InteractionContext context)
        {
            return OnMouseMove(point, context);
        }

        /// <summary>
        /// Public method to handle mouse up events from external callers.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        public bool ProcessMouseUp(SKPoint point, InteractionContext context)
        {
            return OnMouseUp(point, context);
        }
    }

    /// <summary>
    /// Defines the types of UML diagrams.
    /// </summary>
    public enum DiagramType
    {
        /// <summary>
        /// Class diagram.
        /// </summary>
        Class,

        /// <summary>
        /// Sequence diagram.
        /// </summary>
        Sequence,

        /// <summary>
        /// Use case diagram.
        /// </summary>
        UseCase,

        /// <summary>
        /// Activity diagram.
        /// </summary>
        Activity,

        /// <summary>
        /// State diagram.
        /// </summary>
        State,

        /// <summary>
        /// Component diagram.
        /// </summary>
        Component,

        /// <summary>
        /// Deployment diagram.
        /// </summary>
        Deployment
    }
}