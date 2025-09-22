using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Visual workflow designer canvas that provides drag-drop functionality, connection management,
    /// and workflow execution controls for automation components.
    /// </summary>
    public class WorkflowCanvas : SkiaComponent
    {
        #region Private Fields
        private List<AutomationNode> _automationNodes = new List<AutomationNode>();
        private List<WorkflowConnection> _connections = new List<WorkflowConnection>();
        private AutomationNode _selectedNode;
        private AutomationNode _draggedNode;
        private SKPoint _dragOffset;
        private WorkflowConnection _draggedConnection;
        private ConnectionPoint _connectionStart;
        private SKPoint _mousePosition;
        private bool _isDragging;
        private bool _isConnecting;
        private float _zoomLevel = 1.0f;
        private SKPoint _panOffset = SKPoint.Empty;
        private bool _isPanning;
        private SKPoint _panStartPosition;
        private bool _showGrid = true;
        private float _gridSize = 20f;
        private WorkflowExecutionState _executionState = WorkflowExecutionState.Idle;
        private Dictionary<string, object> _workflowVariables = new Dictionary<string, object>();
        #endregion

        #region Events
        /// <summary>
        /// Event raised when a node is selected.
        /// </summary>
        public event EventHandler<NodeSelectedEventArgs> NodeSelected;

        /// <summary>
        /// Event raised when a connection is created.
        /// </summary>
        public event EventHandler<ConnectionCreatedEventArgs> ConnectionCreated;

        /// <summary>
        /// Event raised when a connection is removed.
        /// </summary>
        public new event EventHandler<ConnectionRemovedEventArgs> ConnectionRemoved;

        /// <summary>
        /// Event raised when workflow execution state changes.
        /// </summary>
        public event EventHandler<WorkflowExecutionStateChangedEventArgs> ExecutionStateChanged;

        /// <summary>
        /// Event raised when a node is added to the canvas.
        /// </summary>
        public event EventHandler<NodeAddedEventArgs> NodeAdded;

        /// <summary>
        /// Event raised when a node is removed from the canvas.
        /// </summary>
        public event EventHandler<NodeRemovedEventArgs> NodeRemoved;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the collection of automation nodes in the workflow.
        /// </summary>
        public IReadOnlyList<AutomationNode> AutomationNodes => _automationNodes.AsReadOnly();

        /// <summary>
        /// Gets the collection of connections between nodes.
        /// </summary>
        public IReadOnlyList<WorkflowConnection> Connections => _connections.AsReadOnly();

        /// <summary>
        /// Gets or sets the currently selected node.
        /// </summary>
        public AutomationNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnNodeSelected(new NodeSelectedEventArgs { Node = value });
                }
            }
        }

        /// <summary>
        /// Gets or sets the zoom level of the canvas.
        /// </summary>
        public float ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                var newZoom = Math.Max(0.1f, Math.Min(5.0f, value));
                if (Math.Abs(_zoomLevel - newZoom) > 0.01f)
                {
                    _zoomLevel = newZoom;
                }
            }
        }

        /// <summary>
        /// Gets or sets the pan offset of the canvas.
        /// </summary>
        public SKPoint PanOffset
        {
            get => _panOffset;
            set
            {
                if (_panOffset != value)
                {
                    _panOffset = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the grid background.
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (_showGrid != value)
                {
                    _showGrid = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the grid size.
        /// </summary>
        public float GridSize
        {
            get => _gridSize;
            set
            {
                if (Math.Abs(_gridSize - value) > 0.01f)
                {
                    _gridSize = Math.Max(10f, value);
                }
            }
        }

        /// <summary>
        /// Gets the current workflow execution state.
        /// </summary>
        public WorkflowExecutionState ExecutionState
        {
            get => _executionState;
            private set
            {
                if (_executionState != value)
                {
                    var oldState = _executionState;
                    _executionState = value;
                    OnExecutionStateChanged(new WorkflowExecutionStateChangedEventArgs
                    {
                        OldState = oldState,
                        NewState = value
                    });
                }
            }
        }

        /// <summary>
        /// Gets or sets the workflow variables.
        /// </summary>
        public Dictionary<string, object> WorkflowVariables
        {
            get => _workflowVariables;
            set => _workflowVariables = value ?? new Dictionary<string, object>();
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the WorkflowCanvas class.
        /// </summary>
        public WorkflowCanvas()
        {
            Width = 800;
            Height = 600;
        }
        #endregion

        #region Node Management
        /// <summary>
        /// Adds an automation node to the canvas.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <param name="position">The position to place the node.</param>
        public void AddNode(AutomationNode node, SKPoint position)
        {
            if (node == null) return;

            node.X = position.X;
            node.Y = position.Y;
            _automationNodes.Add(node);

            OnNodeAdded(new NodeAddedEventArgs { Node = node });
        }

        /// <summary>
        /// Removes an automation node from the canvas.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        public void RemoveNode(AutomationNode node)
        {
            if (node == null || !_automationNodes.Contains(node)) return;

            // Remove all connections involving this node
            var connectionsToRemove = _connections.Where(c => c.FromNode == node || c.ToNode == node).ToList();
            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }

            _automationNodes.Remove(node);
            
            if (_selectedNode == node)
                SelectedNode = null;

            OnNodeRemoved(new NodeRemovedEventArgs { Node = node });
        }

        /// <summary>
        /// Gets the node at the specified position.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns>The node at the position, or null if none found.</returns>
        public AutomationNode GetNodeAtPosition(SKPoint position)
        {
            var canvasPosition = ScreenToCanvas(position);
            return _automationNodes.LastOrDefault(node => node.Bounds.Contains(canvasPosition));
        }

        /// <summary>
        /// Arranges nodes automatically in a grid layout.
        /// </summary>
        public void ArrangeNodesInGrid(float spacing = 150f)
        {
            var cols = (int)Math.Ceiling(Math.Sqrt(_automationNodes.Count));
            var rows = (int)Math.Ceiling((double)_automationNodes.Count / cols);

            for (int i = 0; i < _automationNodes.Count; i++)
            {
                var col = i % cols;
                var row = i / cols;
                
                var x = 50 + col * spacing;
                var y = 50 + row * spacing;

                _automationNodes[i].X = x;
                _automationNodes[i].Y = y;
            }
        }
        #endregion

        #region Connection Management
        /// <summary>
        /// Creates a connection between two nodes.
        /// </summary>
        /// <param name="fromNode">The source node.</param>
        /// <param name="toNode">The target node.</param>
        /// <param name="fromPoint">The source connection point.</param>
        /// <param name="toPoint">The target connection point.</param>
        public void CreateConnection(AutomationNode fromNode, AutomationNode toNode, 
                                   ConnectionPoint fromPoint, ConnectionPoint toPoint)
        {
            if (fromNode == null || toNode == null || fromNode == toNode) return;

            // Check if connection already exists
            if (_connections.Any(c => c.FromNode == fromNode && c.ToNode == toNode &&
                               c.FromPoint == fromPoint && c.ToPoint == toPoint))
                return;

            var connection = new WorkflowConnection
            {
                FromNode = fromNode,
                ToNode = toNode,
                FromPoint = fromPoint,
                ToPoint = toPoint
            };

            _connections.Add(connection);
            OnConnectionCreated(new ConnectionCreatedEventArgs { Connection = connection });
        }

        /// <summary>
        /// Removes a connection.
        /// </summary>
        /// <param name="connection">The connection to remove.</param>
        public void RemoveConnection(WorkflowConnection connection)
        {
            if (connection == null || !_connections.Contains(connection)) return;

            _connections.Remove(connection);
            OnConnectionRemoved(new ConnectionRemovedEventArgs { Connection = connection });
        }

        /// <summary>
        /// Gets the connection at the specified position.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <param name="tolerance">The tolerance for hit detection.</param>
        /// <returns>The connection at the position, or null if none found.</returns>
        public WorkflowConnection GetConnectionAtPosition(SKPoint position, float tolerance = 5f)
        {
            var canvasPosition = ScreenToCanvas(position);
            
            return _connections.FirstOrDefault(connection =>
                IsPointOnConnection(canvasPosition, connection, tolerance));
        }

        /// <summary>
        /// Checks if a point is on a connection line.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <param name="connection">The connection to check against.</param>
        /// <param name="tolerance">The tolerance for hit detection.</param>
        /// <returns>True if the point is on the connection.</returns>
        private bool IsPointOnConnection(SKPoint point, WorkflowConnection connection, float tolerance)
        {
            var fromPos = GetConnectionPointPosition(connection.FromNode, connection.FromPoint);
            var toPos = GetConnectionPointPosition(connection.ToNode, connection.ToPoint);

            return DistancePointToLine(point, fromPos, toPos) <= tolerance;
        }

        /// <summary>
        /// Calculates the distance from a point to a line.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="lineStart">The line start point.</param>
        /// <param name="lineEnd">The line end point.</param>
        /// <returns>The distance from point to line.</returns>
        private float DistancePointToLine(SKPoint point, SKPoint lineStart, SKPoint lineEnd)
        {
            var A = point.X - lineStart.X;
            var B = point.Y - lineStart.Y;
            var C = lineEnd.X - lineStart.X;
            var D = lineEnd.Y - lineStart.Y;

            var dot = A * C + B * D;
            var lenSq = C * C + D * D;

            if (lenSq == 0) return (float)Math.Sqrt(A * A + B * B);

            var param = dot / lenSq;

            float xx, yy;

            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }

            var dx = point.X - xx;
            var dy = point.Y - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
        #endregion

        #region Coordinate Transformation
        /// <summary>
        /// Converts screen coordinates to canvas coordinates.
        /// </summary>
        /// <param name="screenPoint">The screen point.</param>
        /// <returns>The canvas point.</returns>
        public SKPoint ScreenToCanvas(SKPoint screenPoint)
        {
            return new SKPoint(
                (screenPoint.X - _panOffset.X) / _zoomLevel,
                (screenPoint.Y - _panOffset.Y) / _zoomLevel
            );
        }

        /// <summary>
        /// Converts canvas coordinates to screen coordinates.
        /// </summary>
        /// <param name="canvasPoint">The canvas point.</param>
        /// <returns>The screen point.</returns>
        public SKPoint CanvasToScreen(SKPoint canvasPoint)
        {
            return new SKPoint(
                canvasPoint.X * _zoomLevel + _panOffset.X,
                canvasPoint.Y * _zoomLevel + _panOffset.Y
            );
        }
        #endregion

        #region Mouse Interaction
        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            _mousePosition = point;
            var canvasPoint = ScreenToCanvas(point);

            // Check for node selection
            var hitNode = GetNodeAtPosition(point);
            if (hitNode != null)
            {
                SelectedNode = hitNode;
                
                // Check for connection point
                var connectionPoint = GetConnectionPointAtPosition(hitNode, canvasPoint);
                if (connectionPoint != null)
                {
                    StartConnection(hitNode, connectionPoint);
                }
                else
                {
                    StartNodeDrag(hitNode, canvasPoint);
                }
                return true;
            }

            // Check for connection selection
            var hitConnection = GetConnectionAtPosition(point);
            if (hitConnection != null)
            {
                // Select connection (could implement connection properties)
                return true;
            }

            // Start canvas panning
            if ((MouseButton)context.MouseButton == MouseButton.Middle || 
                ((MouseButton)context.MouseButton == MouseButton.Left && context.IsControlPressed))
            {
                StartPanning(point);
                return true;
            }

            // Clear selection
            SelectedNode = null;
            return true;
        }

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            _mousePosition = point;

            if (_isDragging && _draggedNode != null)
            {
                UpdateNodeDrag(point);
            }
            else if (_isConnecting)
            {
                // Redraw connection preview - using base class method
                base.OnMouseMove(point, context);
            }
            else if (_isPanning)
            {
                UpdatePanning(point);
            }

            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            if (_isDragging)
            {
                EndNodeDrag();
            }
            else if (_isConnecting)
            {
                EndConnection(point);
            }
            else if (_isPanning)
            {
                EndPanning();
            }

            return base.OnMouseUp(point, context);
        }

        /// <summary>
        /// Handles mouse wheel events for zooming.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="delta">The wheel delta.</param>
        public void OnMouseWheel(SKPoint point, float delta)
        {
            var zoomFactor = delta > 0 ? 1.1f : 0.9f;
            var newZoom = ZoomLevel * zoomFactor;
            
            // Zoom towards mouse position
            var canvasPoint = ScreenToCanvas(point);
            ZoomLevel = newZoom;
            var newScreenPoint = CanvasToScreen(canvasPoint);
            
            PanOffset = new SKPoint(
                _panOffset.X + (point.X - newScreenPoint.X),
                _panOffset.Y + (point.Y - newScreenPoint.Y)
            );
        }
        #endregion

        #region Drag and Drop Operations
        /// <summary>
        /// Starts dragging a node.
        /// </summary>
        /// <param name="node">The node to drag.</param>
        /// <param name="position">The drag start position.</param>
        private void StartNodeDrag(AutomationNode node, SKPoint position)
        {
            _draggedNode = node;
            _dragOffset = new SKPoint(position.X - node.X, position.Y - node.Y);
            _isDragging = true;
        }

        /// <summary>
        /// Updates node position during drag.
        /// </summary>
        /// <param name="position">The current mouse position.</param>
        private void UpdateNodeDrag(SKPoint position)
        {
            if (_draggedNode == null) return;

            var canvasPosition = ScreenToCanvas(position);
            _draggedNode.X = canvasPosition.X - _dragOffset.X;
            _draggedNode.Y = canvasPosition.Y - _dragOffset.Y;
        }

        /// <summary>
        /// Ends node dragging.
        /// </summary>
        private void EndNodeDrag()
        {
            _isDragging = false;
            _draggedNode = null;
            _dragOffset = SKPoint.Empty;
        }

        /// <summary>
        /// Starts creating a connection.
        /// </summary>
        /// <param name="node">The source node.</param>
        /// <param name="connectionPoint">The source connection point.</param>
        private void StartConnection(AutomationNode node, ConnectionPoint connectionPoint)
        {
            _isConnecting = true;
            _connectionStart = connectionPoint;
            _selectedNode = node;
        }

        /// <summary>
        /// Ends connection creation.
        /// </summary>
        /// <param name="position">The end position.</param>
        private void EndConnection(SKPoint position)
        {
            if (!_isConnecting) return;

            var hitNode = GetNodeAtPosition(position);
            if (hitNode != null && hitNode != _selectedNode)
            {
                var canvasPosition = ScreenToCanvas(position);
                var targetPoint = GetConnectionPointAtPosition(hitNode, canvasPosition);
                
                if (targetPoint != null && CanCreateConnection(_selectedNode, hitNode, _connectionStart, targetPoint))
                {
                    CreateConnection(_selectedNode, hitNode, _connectionStart, targetPoint);
                }
            }

            _isConnecting = false;
            _connectionStart = null;
        }

        /// <summary>
        /// Starts canvas panning.
        /// </summary>
        /// <param name="position">The pan start position.</param>
        private void StartPanning(SKPoint position)
        {
            _isPanning = true;
            _panStartPosition = position;
        }

        /// <summary>
        /// Updates canvas panning.
        /// </summary>
        /// <param name="position">The current mouse position.</param>
        private void UpdatePanning(SKPoint position)
        {
            var deltaX = position.X - _panStartPosition.X;
            var deltaY = position.Y - _panStartPosition.Y;
            
            PanOffset = new SKPoint(_panOffset.X + deltaX, _panOffset.Y + deltaY);
            _panStartPosition = position;
        }

        /// <summary>
        /// Ends canvas panning.
        /// </summary>
        private void EndPanning()
        {
            _isPanning = false;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the connection point at the specified position on a node.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="position">The position to check.</param>
        /// <returns>The connection point, or null if none found.</returns>
        private ConnectionPoint GetConnectionPointAtPosition(AutomationNode node, SKPoint position)
        {
            // This would need to be implemented based on how connection points are defined
            // For now, return a simple output point
            if (position.X > node.X + node.Width - 20 && position.Y > node.Y + node.Height / 2 - 10 && position.Y < node.Y + node.Height / 2 + 10)
            {
                return new ConnectionPoint { Type = ConnectionPointType.Output };
            }
            
            if (position.X < node.X + 20 && position.Y > node.Y + node.Height / 2 - 10 && position.Y < node.Y + node.Height / 2 + 10)
            {
                return new ConnectionPoint { Type = ConnectionPointType.Input };
            }

            return null;
        }

        /// <summary>
        /// Gets the screen position of a connection point.
        /// </summary>
        /// <param name="node">The node containing the connection point.</param>
        /// <param name="connectionPoint">The connection point.</param>
        /// <returns>The screen position of the connection point.</returns>
        private SKPoint GetConnectionPointPosition(AutomationNode node, ConnectionPoint connectionPoint)
        {
            var nodeScreenPos = CanvasToScreen(new SKPoint(node.X, node.Y));
            
            // Simple implementation - would be more sophisticated in real implementation
            if (connectionPoint.Type == ConnectionPointType.Output)
            {
                return new SKPoint(nodeScreenPos.X + node.Width * _zoomLevel, nodeScreenPos.Y + node.Height * _zoomLevel / 2);
            }
            else
            {
                return new SKPoint(nodeScreenPos.X, nodeScreenPos.Y + node.Height * _zoomLevel / 2);
            }
        }

        /// <summary>
        /// Determines if a connection can be created between two nodes.
        /// </summary>
        /// <param name="fromNode">The source node.</param>
        /// <param name="toNode">The target node.</param>
        /// <param name="fromPoint">The source connection point.</param>
        /// <param name="toPoint">The target connection point.</param>
        /// <returns>True if the connection can be created.</returns>
        private bool CanCreateConnection(AutomationNode fromNode, AutomationNode toNode, 
                                       ConnectionPoint fromPoint, ConnectionPoint toPoint)
        {
            // Basic validation
            if (fromNode == toNode) return false;
            if (fromPoint.Type == toPoint.Type) return false; // Can't connect output to output or input to input
            if (_connections.Any(c => c.FromNode == fromNode && c.ToNode == toNode)) return false; // No duplicate connections

            return true;
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draws the workflow canvas content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            canvas.Save();

            // Apply transformation
            canvas.Scale(_zoomLevel);
            canvas.Translate(_panOffset.X / _zoomLevel, _panOffset.Y / _zoomLevel);

            // Draw grid
            if (_showGrid)
            {
                DrawGrid(canvas);
            }

            // Draw connections
            foreach (var connection in _connections)
            {
                DrawConnection(canvas, connection);
            }

            // Draw connection preview
            if (_isConnecting && _connectionStart != null && _selectedNode != null)
            {
                DrawConnectionPreview(canvas);
            }

            // Draw nodes
            foreach (var node in _automationNodes)
            {
                DrawNode(canvas, node);
            }

            canvas.Restore();

            // Draw UI overlay (not affected by zoom/pan)
            DrawUIOverlay(canvas);
        }

        /// <summary>
        /// Draws the grid background.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        private void DrawGrid(SKCanvas canvas)
        {
            using var gridPaint = new SKPaint
            {
                Color = SKColor.Parse("#E0E0E0"),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            };

            var bounds = new SKRect(-_panOffset.X / _zoomLevel, -_panOffset.Y / _zoomLevel,
                                  (Width - _panOffset.X) / _zoomLevel, (Height - _panOffset.Y) / _zoomLevel);

            var startX = (int)(bounds.Left / _gridSize) * _gridSize;
            var startY = (int)(bounds.Top / _gridSize) * _gridSize;

            // Draw vertical lines
            for (float x = startX; x <= bounds.Right; x += _gridSize)
            {
                canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, gridPaint);
            }

            // Draw horizontal lines
            for (float y = startY; y <= bounds.Bottom; y += _gridSize)
            {
                canvas.DrawLine(bounds.Left, y, bounds.Right, y, gridPaint);
            }
        }

        /// <summary>
        /// Draws a workflow connection.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="connection">The connection to draw.</param>
        private void DrawConnection(SKCanvas canvas, WorkflowConnection connection)
        {
            var fromPos = GetConnectionPointPosition(connection.FromNode, connection.FromPoint);
            var toPos = GetConnectionPointPosition(connection.ToNode, connection.ToPoint);

            // Convert back to canvas coordinates
            fromPos = ScreenToCanvas(fromPos);
            toPos = ScreenToCanvas(toPos);

            using var connectionPaint = new SKPaint
            {
                Color = connection.IsActive ? SKColor.Parse("#4CAF50") : SKColor.Parse("#757575"),
                StrokeWidth = connection.IsActive ? 3 : 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw curved connection
            using var path = new SKPath();
            path.MoveTo(fromPos);

            var controlPoint1 = new SKPoint(fromPos.X + 50, fromPos.Y);
            var controlPoint2 = new SKPoint(toPos.X - 50, toPos.Y);
            path.CubicTo(controlPoint1, controlPoint2, toPos);

            canvas.DrawPath(path, connectionPaint);

            // Draw arrow at the end
            DrawArrow(canvas, controlPoint2, toPos, connectionPaint.Color);
        }

        /// <summary>
        /// Draws a connection preview during connection creation.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        private void DrawConnectionPreview(SKCanvas canvas)
        {
            var fromPos = GetConnectionPointPosition(_selectedNode, _connectionStart);
            fromPos = ScreenToCanvas(fromPos);
            var toPos = ScreenToCanvas(_mousePosition);

            using var previewPaint = new SKPaint
            {
                Color = SKColor.Parse("#2196F3"),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
            };

            canvas.DrawLine(fromPos, toPos, previewPaint);
        }

        /// <summary>
        /// Draws an arrow at the end of a connection.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="from">The start point of the arrow.</param>
        /// <param name="to">The end point of the arrow.</param>
        /// <param name="color">The arrow color.</param>
        private void DrawArrow(SKCanvas canvas, SKPoint from, SKPoint to, SKColor color)
        {
            using var arrowPaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            var arrowLength = 8f;
            var arrowAngle = Math.PI / 6;

            var arrowPoint1 = new SKPoint(
                to.X - arrowLength * (float)Math.Cos(angle - arrowAngle),
                to.Y - arrowLength * (float)Math.Sin(angle - arrowAngle)
            );

            var arrowPoint2 = new SKPoint(
                to.X - arrowLength * (float)Math.Cos(angle + arrowAngle),
                to.Y - arrowLength * (float)Math.Sin(angle + arrowAngle)
            );

            using var arrowPath = new SKPath();
            arrowPath.MoveTo(to);
            arrowPath.LineTo(arrowPoint1);
            arrowPath.LineTo(arrowPoint2);
            arrowPath.Close();

            canvas.DrawPath(arrowPath, arrowPaint);
        }

        /// <summary>
        /// Draws a workflow node.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="node">The node to draw.</param>
        private void DrawNode(SKCanvas canvas, AutomationNode node)
        {
            canvas.Save();
            canvas.Translate(node.X, node.Y);

            // Highlight selected node
            if (node == _selectedNode)
            {
                using var selectionPaint = new SKPaint
                {
                    Color = SKColor.Parse("#2196F3"),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3,
                    IsAntialias = true
                };

                var selectionRect = new SKRect(-2, -2, node.Width + 2, node.Height + 2);
                canvas.DrawRoundRect(selectionRect, 5, 5, selectionPaint);
            }

            // Draw the node itself
            var nodeContext = new DrawingContext
            {
                Canvas = canvas,
                Bounds = new SKRect(0, 0, node.Width, node.Height),
                MousePosition = ScreenToCanvas(_mousePosition),
                Parent = this
            };

            node.DrawNode(canvas, nodeContext);

            canvas.Restore();
        }

        /// <summary>
        /// Draws the UI overlay (zoom level, execution state, etc.).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        private void DrawUIOverlay(SKCanvas canvas)
        {
            using var overlayPaint = new SKPaint
            {
                Color = SKColor.Parse("#333333"),
                IsAntialias = true
            };

            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 12);

            // Draw zoom level
            var zoomText = $"Zoom: {_zoomLevel:P0}";
            canvas.DrawText(zoomText, 10, Height - 30, font, overlayPaint);

            // Draw execution state
            var stateText = $"State: {_executionState}";
            var stateColor = _executionState switch
            {
                WorkflowExecutionState.Running => SKColors.Green,
                WorkflowExecutionState.Failed => SKColors.Red,
                WorkflowExecutionState.Paused => SKColors.Orange,
                _ => SKColors.Gray
            };

            using var statePaint = new SKPaint { Color = stateColor, IsAntialias = true };
            canvas.DrawText(stateText, 10, Height - 10, font, statePaint);

            // Draw node count
            var nodeCountText = $"Nodes: {_automationNodes.Count}";
            canvas.DrawText(nodeCountText, 150, Height - 30, font, overlayPaint);

            // Draw connection count
            var connectionCountText = $"Connections: {_connections.Count}";
            canvas.DrawText(connectionCountText, 150, Height - 10, font, overlayPaint);
        }
        #endregion

        #region Event Raising
        /// <summary>
        /// Raises the NodeSelected event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnNodeSelected(NodeSelectedEventArgs e)
        {
            NodeSelected?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the ConnectionCreated event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnConnectionCreated(ConnectionCreatedEventArgs e)
        {
            ConnectionCreated?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the ConnectionRemoved event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnConnectionRemoved(ConnectionRemovedEventArgs e)
        {
            ConnectionRemoved?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the ExecutionStateChanged event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnExecutionStateChanged(WorkflowExecutionStateChangedEventArgs e)
        {
            ExecutionStateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the NodeAdded event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnNodeAdded(NodeAddedEventArgs e)
        {
            NodeAdded?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the NodeRemoved event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnNodeRemoved(NodeRemovedEventArgs e)
        {
            NodeRemoved?.Invoke(this, e);
        }
        #endregion
    }

    #region Supporting Classes and Enums
    /// <summary>
    /// Represents a connection between two automation nodes.
    /// </summary>
    public class WorkflowConnection
    {
        /// <summary>
        /// Gets or sets the source node.
        /// </summary>
        public AutomationNode FromNode { get; set; }

        /// <summary>
        /// Gets or sets the target node.
        /// </summary>
        public AutomationNode ToNode { get; set; }

        /// <summary>
        /// Gets or sets the source connection point.
        /// </summary>
        public ConnectionPoint FromPoint { get; set; }

        /// <summary>
        /// Gets or sets the target connection point.
        /// </summary>
        public ConnectionPoint ToPoint { get; set; }

        /// <summary>
        /// Gets or sets whether the connection is currently active (data flowing).
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets additional connection metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Workflow execution states.
    /// </summary>
    public enum WorkflowExecutionState
    {
        Idle,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Event arguments for node selection events.
    /// </summary>
    public class NodeSelectedEventArgs : EventArgs
    {
        public AutomationNode Node { get; set; }
    }

    /// <summary>
    /// Event arguments for connection creation events.
    /// </summary>
    public class ConnectionCreatedEventArgs : EventArgs
    {
        public WorkflowConnection Connection { get; set; }
    }

    /// <summary>
    /// Event arguments for connection removal events.
    /// </summary>
    public class ConnectionRemovedEventArgs : EventArgs
    {
        public WorkflowConnection Connection { get; set; }
    }

    /// <summary>
    /// Event arguments for execution state change events.
    /// </summary>
    public class WorkflowExecutionStateChangedEventArgs : EventArgs
    {
        public WorkflowExecutionState OldState { get; set; }
        public WorkflowExecutionState NewState { get; set; }
    }

    /// <summary>
    /// Event arguments for node added events.
    /// </summary>
    public class NodeAddedEventArgs : EventArgs
    {
        public AutomationNode Node { get; set; }
    }

    /// <summary>
    /// Event arguments for node removed events.
    /// </summary>
    public class NodeRemovedEventArgs : EventArgs
    {
        public AutomationNode Node { get; set; }
    }
    #endregion
}