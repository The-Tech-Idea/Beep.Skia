using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Helper class for handling user interactions in the drawing manager.
    /// </summary>
    public class InteractionHelper
    {
        private readonly DrawingManager _drawingManager;
        private bool _isDragging;
        private bool _isDraggingLine;
        private bool _isSelecting;
        private bool _isPanning;
        private bool _isDrawingLine;
        private SkiaComponent _draggingComponent;
        private IConnectionLine _draggingLine;
        private SKPoint _draggingOffset;
        private SKPoint _mousePosition;
        private SKPoint _selectionStart;
        private SKRect _selectionRect;
        private IConnectionPoint _sourcePoint;
    // Hover tracking
    private IConnectionLine _hoveredLine;
    private SkiaComponent _hoveredComponent;
    // resizing state
    private bool _isResizing;
    private string _resizeHandle;
    private SKRect _initialComponentBounds;
    private SKPoint _mouseDownCanvas;
    // Component that has consumed the current mouse interaction (child components can handle their own drag)
    private SkiaComponent _componentHandlingMouse;

    // Double-click detection
    private DateTime _lastClickTime = DateTime.MinValue;
    private SKPoint _lastClickPosition;
    private SkiaComponent _lastClickedComponent;
    private IConnectionLine _lastClickedLine;
    private const double DoubleClickThresholdMs = 500; // 500ms for double-click
    private const float DoubleClickDistanceThreshold = 5f; // 5 pixels tolerance

        /// <summary>
        /// Gets a value indicating whether a component is being dragged.
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// Gets a value indicating whether a line is being dragged.
        /// </summary>
        public bool IsDraggingLine => _isDraggingLine;

        /// <summary>
        /// Gets a value indicating whether a selection is being made.
        /// </summary>
        public bool IsSelecting => _isSelecting;

        /// <summary>
        /// Gets a value indicating whether the canvas is being panned.
        /// </summary>
        public bool IsPanning => _isPanning;

        /// <summary>
        /// Gets a value indicating whether a line is being drawn.
        /// </summary>
        public bool IsDrawingLine => _isDrawingLine;

        /// <summary>
        /// Gets the currently dragged component.
        /// </summary>
        public SkiaComponent DraggingComponent => _draggingComponent;

        /// <summary>
        /// Gets the currently dragged line.
        /// </summary>
        public IConnectionLine DraggingLine => _draggingLine;

        /// <summary>
        /// Gets the current mouse position.
        /// </summary>
        public SKPoint MousePosition => _mousePosition;

        /// <summary>
        /// Gets the current selection rectangle.
        /// </summary>
        public SKRect SelectionRect => _selectionRect;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionHelper"/> class.
        /// </summary>
        /// <param name="drawingManager">The drawing manager that owns this interaction helper.</param>
        public InteractionHelper(DrawingManager drawingManager)
        {
            _drawingManager = drawingManager;
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The point where the mouse down occurred.</param>
        /// <param name="modifiers">Keyboard modifiers.</param>
        /// <param name="mouseButton">Mouse button pressed (0=left, 1=right, 2=middle).</param>
        public void HandleMouseDown(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None, int mouseButton = 0)
        {
            // Convert screen point to canvas point (world space)
            var canvasPoint = ScreenToCanvas(point);

            // 1) Prioritize connection point hit-testing at the exact cursor position
            var immediateCp = GetConnectionPointAt(canvasPoint);
            if (immediateCp != null)
            {
                // Begin drag-to-connect from a connection point immediately (no Shift required)
                _drawingManager.SelectionManager.ClearSelection();
                _drawingManager.SelectionManager.SelectConnectionPoint(immediateCp);
                _isDrawingLine = true;
                _sourcePoint = immediateCp;
                _mouseDownCanvas = canvasPoint;
                StartDrawingLine(immediateCp, canvasPoint);
                return;
            }

            // 2) Give the top-most component a chance to handle the mouse down first
            var topComponent = GetComponentAt(canvasPoint);
            if (topComponent != null)
            {
                var ctx = new InteractionContext
                {
                    MousePosition = (topComponent.IsStatic ? point : canvasPoint),
                    Modifiers = (int)modifiers,
                    Bounds = topComponent.Bounds
                };

                try
                {
                    var ptForComp = topComponent.IsStatic ? point : canvasPoint;
                    if (topComponent.HandleMouseDown(ptForComp, ctx))
                    {
                        // Component handled the interaction; remember it and return
                        _componentHandlingMouse = topComponent;
                        return;
                    }
                }
                catch
                {
                    // swallow component exceptions to keep interaction robust
                }
            }

            // Check for line hit (allow arrow drag or line selection)
            var line = GetLineAt(canvasPoint);
            if (line != null)
            {
                var arrow = GetArrowAt(line, canvasPoint);
                if (arrow != null)
                {
                    _isDraggingLine = true;
                    _draggingLine = line;
                    _sourcePoint = arrow == "start" ? line.Start : line.End;
                    return;
                }
                // If not on arrow, treat as selection toggle/select
                if ((modifiers & SKKeyModifiers.Control) == SKKeyModifiers.Control)
                {
                    if (_drawingManager.SelectionManager.IsSelected(line))
                        _drawingManager.SelectionManager.RemoveFromSelection(line);
                    else
                        _drawingManager.SelectionManager.SelectLine(line, addToSelection: true);
                }
                else if (!_drawingManager.SelectionManager.IsSelected(line))
                {
                    _drawingManager.SelectionManager.ClearSelection();
                    _drawingManager.SelectionManager.SelectLine(line);
                }
                return;
            }

            // Check for component interaction
            var component = GetComponentAt(canvasPoint);
            if (component != null)
            {
                // Respect static components: do not start drag/resize OR selection for overlays (palette/property editor)
                if (component is SkiaComponent scStatic && scStatic.IsStatic)
                {
                    // Let the static overlay handle its own interactions (already called above), but do not affect canvas selection
                    return;
                }
                // Check if clicking on a resize handle
                var handle = GetResizeHandleAt(component, canvasPoint);
                if (handle != null)
                {
                    // Start resizing
                    _isResizing = true;
                    _resizeHandle = handle;
                    _draggingComponent = component;
                    _initialComponentBounds = component.Bounds;
                    _mouseDownCanvas = canvasPoint;
                    return;
                }

                // Regular component dragging
                _isDragging = true;
                _draggingComponent = component;
                _draggingOffset = canvasPoint - new SKPoint(component.X, component.Y);

                // Handle selection
                if (modifiers.HasFlag(SKKeyModifiers.Control))
                {
                    // Toggle selection
                    if (_drawingManager.SelectionManager.IsSelected(component))
                    {
                        _drawingManager.SelectionManager.RemoveFromSelection(component);
                    }
                    else
                    {
                        _drawingManager.SelectionManager.SelectComponent(component, true);
                    }
                }
                else if (!_drawingManager.SelectionManager.IsSelected(component))
                {
                    // Select this component only
                    _drawingManager.SelectionManager.ClearSelection();
                    _drawingManager.SelectionManager.SelectComponent(component);
                }
            }
            else
            {
                // Start selection rectangle in world space
                _isSelecting = true;
                _selectionStart = canvasPoint;
                _selectionRect = new SKRect(canvasPoint.X, canvasPoint.Y, canvasPoint.X, canvasPoint.Y);
            }
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The point where the mouse up occurred.</param>
        /// <param name="modifiers">Keyboard modifiers.</param>
        /// <param name="mouseButton">Mouse button released (0=left, 1=right, 2=middle).</param>
        public void HandleMouseUp(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None, int mouseButton = 0)
        {
            var canvasPoint = ScreenToCanvas(point);

            // If a component consumed the mouse interaction, forward the up and clear
            if (_componentHandlingMouse != null)
            {
                var ctx = new InteractionContext { MousePosition = (_componentHandlingMouse.IsStatic ? point : canvasPoint), Modifiers = (int)modifiers };
                try { _componentHandlingMouse.HandleMouseUp(_componentHandlingMouse.IsStatic ? point : canvasPoint, ctx); } catch { }
                _componentHandlingMouse = null;
                return;
            }

            // Track whether this was a simple click (not a drag)
            bool wasSimpleClick = !_isDragging && !_isDraggingLine && !_isResizing && !_isDrawingLine && !_isSelecting;

            if (_isDraggingLine && _draggingLine != null)
            {
                _isDraggingLine = false;

                // Try to connect to a new connection point
                var targetPoint = GetConnectionPointAt(canvasPoint);
                if (targetPoint != null && targetPoint != _sourcePoint)
                {
                    // Validate the connection
                    if (CanConnectPoints(_sourcePoint, targetPoint))
                    {
                        if (_sourcePoint.Type == ConnectionPointType.Out && targetPoint.Type == ConnectionPointType.In)
                        {
                            _drawingManager.MoveConnectionLine(_draggingLine, null, targetPoint);
                        }
                        else if (_sourcePoint.Type == ConnectionPointType.In && targetPoint.Type == ConnectionPointType.Out)
                        {
                            _drawingManager.MoveConnectionLine(_draggingLine, targetPoint, null);
                        }
                    }
                    // If connection is invalid, the line will be discarded
                }

                _draggingLine = null;
                _sourcePoint = null;
            }
            else if (_isResizing && _draggingComponent != null)
            {
                // Finish resizing
                _isResizing = false;
                _resizeHandle = null;
                _draggingComponent = null;
            }
            else if (_isDragging && _draggingComponent != null)
            {
                _isDragging = false;
                // Fire drop event with final positions
                try
                {
                    var screenPt = point;
                    var canvasPt = ScreenToCanvas(point);
                    var dropArgs = new ComponentDropEventArgs
                    {
                        Component = _draggingComponent,
                        ScreenPosition = screenPt,
                        CanvasPosition = canvasPt,
                        Bounds = _draggingComponent.Bounds
                    };
                    _drawingManager.RaiseComponentDropped(dropArgs);
                }
                catch { }

                _draggingComponent = null;

                // After a component drag ends, ensure selection reflects the single component under cursor
                try
                {
                    var compAtUp = GetComponentAt(canvasPoint);
                    if (compAtUp != null)
                    {
                        // If only one component is intended, select it to drive property editor refresh
                        if (!_drawingManager.SelectionManager.IsSelected(compAtUp) || _drawingManager.SelectionManager.SelectedComponents.Count != 1)
                        {
                            _drawingManager.SelectionManager.ClearSelection();
                            _drawingManager.SelectionManager.SelectComponent(compAtUp);
                        }
                    }
                }
                catch { }
            }
            else if (_isDrawingLine)
            {
                _isDrawingLine = false;
                var targetPoint = GetConnectionPointAt(canvasPoint);
                var line = _drawingManager.CurrentLine;
                // Allow connection when:
                // - both points exist
                // - they belong to different components
                // - these specific points aren’t already connected by an existing line
                if (targetPoint != null && _sourcePoint != null &&
                    _sourcePoint.Component != targetPoint.Component &&
                    !PointsAlreadyConnected(_sourcePoint, targetPoint))
                {
                    try
                    {
                        // Finalize the preview line onto the target port
                        if (line != null)
                        {
                            line.End = targetPoint;
                            _drawingManager.AddLine(line);
                        }
                        // Link connection points and components
                        try { _sourcePoint.ConnectTo(targetPoint); } catch { }
                        try { targetPoint.ConnectTo(_sourcePoint); } catch { }
                        try
                        {
                            if (_sourcePoint.Component is SkiaComponent scA && targetPoint.Component is SkiaComponent scB)
                            {
                                scA.ConnectTo(scB);
                                scB.ConnectTo(scA);
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
                // Clear current line preview regardless of success
                try { _drawingManager.CurrentLine = null; } catch { }
                _sourcePoint = null;
            }
            else if (_isSelecting)
            {
                _isSelecting = false;

                // Select components within selection rectangle
                _drawingManager.SelectionManager.SelectComponentsInRect(_selectionRect, _drawingManager.Components, modifiers.HasFlag(SKKeyModifiers.Control));
            }

            // ========== Click/DoubleClick/RightClick Detection ==========
            // Only fire click events for simple clicks (not drags/draws/selections)
            if (wasSimpleClick)
            {
                var now = DateTime.Now;
                var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;
                var distanceFromLastClick = Distance(canvasPoint, _lastClickPosition);
                bool isDoubleClick = timeSinceLastClick < DoubleClickThresholdMs && distanceFromLastClick < DoubleClickDistanceThreshold;

                // Check what was clicked
                var clickedComponent = GetComponentAt(canvasPoint);
                var clickedLine = GetLineAt(canvasPoint);
                
                if (clickedComponent != null)
                {
                    // Component was clicked
                    var args = new ComponentInteractionEventArgs(
                        clickedComponent,
                        canvasPoint,
                        point,
                        mouseButton,
                        modifiers,
                        isDoubleClick ? InteractionType.DoubleClick : InteractionType.Click
                    );

                    if (mouseButton == 1) // Right-click
                    {
                        var rightClickArgs = new ComponentInteractionEventArgs(
                            clickedComponent, canvasPoint, point, mouseButton, modifiers, InteractionType.RightClick);
                        _drawingManager.RaiseComponentRightClicked(rightClickArgs);
                    }
                    else if (isDoubleClick && _lastClickedComponent == clickedComponent)
                    {
                        _drawingManager.RaiseComponentDoubleClicked(args);
                        // Reset double-click tracker
                        _lastClickTime = DateTime.MinValue;
                        _lastClickedComponent = null;
                    }
                    else
                    {
                        _drawingManager.RaiseComponentClicked(args);
                        // Track for potential double-click
                        _lastClickTime = now;
                        _lastClickPosition = canvasPoint;
                        _lastClickedComponent = clickedComponent;
                        _lastClickedLine = null;
                    }
                }
                else if (clickedLine != null)
                {
                    // Line was clicked
                    var args = new LineInteractionEventArgs(
                        clickedLine,
                        canvasPoint,
                        point,
                        mouseButton,
                        modifiers,
                        isDoubleClick ? InteractionType.DoubleClick : InteractionType.Click,
                        false // isArrowClick - could enhance to detect arrow clicks
                    );

                    if (mouseButton == 1) // Right-click
                    {
                        var rightClickArgs = new LineInteractionEventArgs(
                            clickedLine, canvasPoint, point, mouseButton, modifiers, InteractionType.RightClick, false);
                        _drawingManager.RaiseLineRightClicked(rightClickArgs);
                    }
                    else if (isDoubleClick && _lastClickedLine == clickedLine)
                    {
                        _drawingManager.RaiseLineDoubleClicked(args);
                        // Reset double-click tracker
                        _lastClickTime = DateTime.MinValue;
                        _lastClickedLine = null;
                    }
                    else
                    {
                        _drawingManager.RaiseLineClicked(args);
                        // Track for potential double-click
                        _lastClickTime = now;
                        _lastClickPosition = canvasPoint;
                        _lastClickedLine = clickedLine;
                        _lastClickedComponent = null;
                    }
                }
                else
                {
                    // Empty canvas was clicked
                    var args = new DiagramInteractionEventArgs(
                        canvasPoint,
                        point,
                        mouseButton,
                        modifiers,
                        isDoubleClick ? InteractionType.DoubleClick : InteractionType.Click
                    );

                    if (mouseButton == 1) // Right-click
                    {
                        var rightClickArgs = new DiagramInteractionEventArgs(
                            canvasPoint, point, mouseButton, modifiers, InteractionType.RightClick);
                        _drawingManager.RaiseDiagramRightClicked(rightClickArgs);
                    }
                    else if (isDoubleClick)
                    {
                        _drawingManager.RaiseDiagramDoubleClicked(args);
                        // Reset double-click tracker
                        _lastClickTime = DateTime.MinValue;
                    }
                    else
                    {
                        _drawingManager.RaiseDiagramClicked(args);
                        // Track for potential double-click
                        _lastClickTime = now;
                        _lastClickPosition = canvasPoint;
                        _lastClickedComponent = null;
                        _lastClickedLine = null;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates distance between two points.
        /// </summary>
        private float Distance(SKPoint a, SKPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="point">The point where the mouse move occurred.</param>
        /// <param name="modifiers">Keyboard modifiers.</param>
        public void HandleMouseMove(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None)
        {
            var canvasPoint = ScreenToCanvas(point);
            _mousePosition = canvasPoint;

            // If a component consumed the mouse interaction, forward the move
            if (_componentHandlingMouse != null)
            {
                var ctx = new InteractionContext { MousePosition = (_componentHandlingMouse.IsStatic ? point : canvasPoint), Modifiers = (int)modifiers };
                try { _componentHandlingMouse.HandleMouseMove(_componentHandlingMouse.IsStatic ? point : canvasPoint, ctx); } catch { }
                return;
            }

            if (_isDraggingLine && _draggingLine != null)
            {
                // Update line end point for visual feedback
                _draggingLine.EndPoint = canvasPoint;
            }
            else if (_isResizing && _draggingComponent != null)
            {
                // Resize based on the grabbed handle and mouse delta
                var dx = canvasPoint.X - _mouseDownCanvas.X;
                var dy = canvasPoint.Y - _mouseDownCanvas.Y;
                var left = _initialComponentBounds.Left;
                var top = _initialComponentBounds.Top;
                var right = _initialComponentBounds.Right;
                var bottom = _initialComponentBounds.Bottom;

                float minW = 10f, minH = 10f;

                switch (_resizeHandle)
                {
                    case "top-left":
                        left += dx; top += dy; break;
                    case "top-right":
                        right += dx; top += dy; break;
                    case "bottom-right":
                        right += dx; bottom += dy; break;
                    case "bottom-left":
                        left += dx; bottom += dy; break;
                }

                // Normalize to ensure min size
                float newX = Math.Min(left, right);
                float newY = Math.Min(top, bottom);
                float newW = Math.Max(minW, Math.Abs(right - left));
                float newH = Math.Max(minH, Math.Abs(bottom - top));

                // Apply grid snapping to size/pos if enabled
                if (_drawingManager.SnapToGrid)
                {
                    var snappedPos = SnapToGridPoint(new SKPoint(newX, newY));
                    newX = snappedPos.X; newY = snappedPos.Y;
                    // sizes: snap the far corner relative to snapped origin
                    var snappedFar = SnapToGridPoint(new SKPoint(newX + newW, newY + newH));
                    newW = Math.Max(minW, snappedFar.X - newX);
                    newH = Math.Max(minH, snappedFar.Y - newY);
                }

                // Assign size then move to trigger bounds update
                try
                {
                    _draggingComponent.Width = newW;
                    _draggingComponent.Height = newH;
                    _draggingComponent.Move(newX, newY);
                }
                catch { }
            }
            else if (_isDragging && _draggingComponent != null)
            {
                // If the captured component is static, cancel movement updates
                if (_draggingComponent.IsStatic)
                {
                    return;
                }
                var newPosition = canvasPoint - _draggingOffset;
                if (_drawingManager.SnapToGrid)
                {
                    newPosition = SnapToGridPoint(newPosition);
                }

                var offset = newPosition - new SKPoint(_draggingComponent.X, _draggingComponent.Y);
                _drawingManager.MoveSelectedComponents(offset);
            }
            else if (_isDrawingLine)
            {
                var line = _drawingManager.CurrentLine;
                if (line != null) line.EndPoint = canvasPoint;
            }
            else if (_isSelecting)
            {
                // Update selection rectangle
                _selectionRect = new SKRect(
                    Math.Min(_selectionStart.X, canvasPoint.X),
                    Math.Min(_selectionStart.Y, canvasPoint.Y),
                    Math.Max(_selectionStart.X, canvasPoint.X),
                    Math.Max(_selectionStart.Y, canvasPoint.Y)
                );
            }

            // Update hovered line state (when not dragging drawing/resizing)
            if (!_isDragging && !_isDraggingLine && !_isResizing && !_isDrawingLine)
            {
                // Track line hover changes
                var newHoveredLine = GetLineAt(canvasPoint);
                if (!ReferenceEquals(newHoveredLine, _hoveredLine))
                {
                    // Line hover leave
                    if (_hoveredLine != null)
                    {
                        try 
                        { 
                            _hoveredLine.IsHovered = false;
                            var leaveArgs = new LineInteractionEventArgs(
                                _hoveredLine,
                                canvasPoint,
                                point,
                                0,
                                modifiers,
                                InteractionType.HoverLeave
                            );
                            _drawingManager.RaiseLineHoverChanged(leaveArgs);
                        } 
                        catch { }
                    }
                    
                    _hoveredLine = newHoveredLine;
                    
                    // Line hover enter
                    if (_hoveredLine != null)
                    {
                        try 
                        { 
                            _hoveredLine.IsHovered = true;
                            var enterArgs = new LineInteractionEventArgs(
                                _hoveredLine,
                                canvasPoint,
                                point,
                                0,
                                modifiers,
                                InteractionType.HoverEnter
                            );
                            _drawingManager.RaiseLineHoverChanged(enterArgs);
                        } 
                        catch { }
                    }
                    
                    // Request redraw to reflect hover change
                    try { _drawingManager.RequestRedraw(); } catch { }
                }

                // Track component hover changes (if no line is hovered)
                if (_hoveredLine == null)
                {
                    var newHoveredComponent = GetComponentAt(canvasPoint);
                    if (!ReferenceEquals(newHoveredComponent, _hoveredComponent))
                    {
                        // Component hover leave
                        if (_hoveredComponent != null)
                        {
                            try
                            {
                                var leaveArgs = new ComponentInteractionEventArgs(
                                    _hoveredComponent,
                                    canvasPoint,
                                    point,
                                    0,
                                    modifiers,
                                    InteractionType.HoverLeave
                                );
                                _drawingManager.RaiseComponentHoverChanged(leaveArgs);
                            }
                            catch { }
                        }

                        _hoveredComponent = newHoveredComponent;

                        // Component hover enter
                        if (_hoveredComponent != null)
                        {
                            try
                            {
                                var enterArgs = new ComponentInteractionEventArgs(
                                    _hoveredComponent,
                                    canvasPoint,
                                    point,
                                    0,
                                    modifiers,
                                    InteractionType.HoverEnter
                                );
                                _drawingManager.RaiseComponentHoverChanged(enterArgs);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles mouse wheel events for zooming.
        /// </summary>
        /// <param name="point">The point where the mouse wheel event occurred.</param>
        /// <param name="delta">The wheel delta value.</param>
        public void HandleMouseWheel(SKPoint point, float delta)
        {
            var zoomFactor = delta > 0 ? 1.1f : 0.9f;
            var newZoom = _drawingManager.Zoom * zoomFactor;

            if (newZoom >= 0.1f && newZoom <= 5.0f)
            {
                // Zoom towards mouse position
                var canvasPoint = ScreenToCanvas(point);
                var scaledCanvasPoint = new SKPoint(canvasPoint.X * newZoom, canvasPoint.Y * newZoom);
                var newPanOffset = new SKPoint(point.X - scaledCanvasPoint.X, point.Y - scaledCanvasPoint.Y);

                _drawingManager.Zoom = newZoom;
                _drawingManager.PanOffset = newPanOffset;
            }
        }

        /// <summary>
        /// Converts screen coordinates to canvas coordinates.
        /// </summary>
        /// <param name="screenPoint">The screen point.</param>
        /// <returns>The canvas point.</returns>
        private SKPoint ScreenToCanvas(SKPoint screenPoint)
        {
            var offset = screenPoint - _drawingManager.PanOffset;
            return new SKPoint(offset.X / _drawingManager.Zoom, offset.Y / _drawingManager.Zoom);
        }

        /// <summary>
        /// Converts canvas coordinates to screen coordinates.
        /// </summary>
        /// <param name="canvasPoint">The canvas point.</param>
        /// <returns>The screen point.</returns>
        private SKPoint CanvasToScreen(SKPoint canvasPoint)
        {
            var scaled = new SKPoint(canvasPoint.X * _drawingManager.Zoom, canvasPoint.Y * _drawingManager.Zoom);
            return scaled + _drawingManager.PanOffset;
        }

        /// <summary>
        /// Snaps a point to the grid.
        /// </summary>
        /// <param name="point">The point to snap.</param>
        /// <returns>The snapped point.</returns>
        private SKPoint SnapToGridPoint(SKPoint point)
        {
            if (!_drawingManager.SnapToGrid) return point;

            return new SKPoint(
                (float)Math.Round(point.X / _drawingManager.GridSpacing) * _drawingManager.GridSpacing,
                (float)Math.Round(point.Y / _drawingManager.GridSpacing) * _drawingManager.GridSpacing
            );
        }

        // Helper methods that delegate to the drawing manager
        private SkiaComponent GetComponentAt(SKPoint point) => _drawingManager.GetComponentAt(point);
        private IConnectionPoint GetConnectionPointAt(SKPoint point) => _drawingManager.GetConnectionPointAt(point);
        private IConnectionLine GetLineAt(SKPoint point) => _drawingManager.GetLineAt(point);
        private string GetArrowAt(IConnectionLine line, SKPoint point) => _drawingManager.GetArrowAt(line, point);

        /// <summary>
        /// Validates whether two connection points can be connected.
        /// </summary>
        /// <param name="sourcePoint">The source connection point.</param>
        /// <param name="targetPoint">The target connection point.</param>
        /// <returns>True if the connection is valid, false otherwise.</returns>
        private bool CanConnectPoints(IConnectionPoint sourcePoint, IConnectionPoint targetPoint)
        {
            // Can't connect to self
            if (sourcePoint == targetPoint)
                return false;

            // Can't connect if either point is not available
            if (!sourcePoint.IsAvailable || !targetPoint.IsAvailable)
                return false;

            // Must connect output to input (data flow direction)
            if (sourcePoint.Type == targetPoint.Type)
                return false;

            // Can't connect points from the same component
            if (sourcePoint.Component == targetPoint.Component)
                return false;

            // Additional validation can be added here for automation nodes
            if (sourcePoint.Component is Components.AutomationNode sourceNode &&
                targetPoint.Component is Components.AutomationNode targetNode)
            {
                // For automation nodes, ensure proper data flow
                if (sourcePoint.Type == ConnectionPointType.Out && targetPoint.Type == ConnectionPointType.In)
                {
                    // Validate node compatibility if needed
                    return true;
                }
                else if (sourcePoint.Type == ConnectionPointType.In && targetPoint.Type == ConnectionPointType.Out)
                {
                    // Reverse connection (less common but allowed)
                    return true;
                }
            }

            return true;
        }
        private string GetResizeHandleAt(SkiaComponent component, SKPoint point) => _drawingManager.GetResizeHandleAt(component, point);
        private void StartDrawingLine(IConnectionPoint sourcePoint, SKPoint point) => _drawingManager.StartDrawingLine(sourcePoint, point);

        /// <summary>
        /// Returns true if a line already exists that connects these two specific connection points (in either direction).
        /// </summary>
        private bool PointsAlreadyConnected(IConnectionPoint a, IConnectionPoint b)
        {
            if (a == null || b == null) return false;
            foreach (var line in _drawingManager.Lines)
            {
                if ((line.Start == a && line.End == b) || (line.Start == b && line.End == a))
                    return true;
            }
            return false;
        }
    }
}
