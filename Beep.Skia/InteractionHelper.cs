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
        private IConnectionLine _currentLine;
    // Component that has consumed the current mouse interaction (child components can handle their own drag)
    private SkiaComponent _componentHandlingMouse;

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
        public void HandleMouseDown(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None)
        {
            // Convert screen point to canvas point
            var canvasPoint = ScreenToCanvas(point);

            // Give the top-most component a chance to handle the mouse down first
            var topComponent = GetComponentAt(canvasPoint);
            if (topComponent != null)
            {
                var ctx = new InteractionContext
                {
                    MousePosition = canvasPoint,
                    Modifiers = (int)modifiers,
                    Bounds = topComponent.Bounds
                };

                try
                {
                    if (topComponent.HandleMouseDown(canvasPoint, ctx))
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

            // Check for line manipulation first
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
            }

            // Check for component interaction
            var component = GetComponentAt(canvasPoint);
            if (component != null)
            {
                // Respect static components: do not start drag/resize on IsStatic
                if (component is SkiaComponent scStatic && scStatic.IsStatic)
                {
                    // Still allow selection toggling, but do not initiate drag/resize
                    if (modifiers.HasFlag(SKKeyModifiers.Control))
                    {
                        if (_drawingManager.SelectionManager.IsSelected(component))
                            _drawingManager.SelectionManager.RemoveFromSelection(component);
                        else
                            _drawingManager.SelectionManager.SelectComponent(component, true);
                    }
                    else if (!_drawingManager.SelectionManager.IsSelected(component))
                    {
                        _drawingManager.SelectionManager.ClearSelection();
                        _drawingManager.SelectionManager.SelectComponent(component);
                    }
                    return;
                }
                // Check if clicking on a resize handle
                var handle = GetResizeHandleAt(component, canvasPoint);
                if (handle != null)
                {
                    // Start resizing
                    _isDragging = true;
                    _draggingComponent = component;
                    _draggingOffset = canvasPoint - new SKPoint(component.Bounds.Left, component.Bounds.Top);
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
                // Check for connection point
                var sourcePoint = GetConnectionPointAt(canvasPoint);
                if (sourcePoint != null)
                {
                    _isDrawingLine = true;
                    StartDrawingLine(sourcePoint, canvasPoint);
                }
                else
                {
                    // Start selection rectangle
                    _isSelecting = true;
                    _selectionStart = canvasPoint;
                    _selectionRect = new SKRect(canvasPoint.X, canvasPoint.Y, canvasPoint.X, canvasPoint.Y);
                }
            }
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The point where the mouse up occurred.</param>
        /// <param name="modifiers">Keyboard modifiers.</param>
        public void HandleMouseUp(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None)
        {
            var canvasPoint = ScreenToCanvas(point);

            // If a component consumed the mouse interaction, forward the up and clear
            if (_componentHandlingMouse != null)
            {
                var ctx = new InteractionContext { MousePosition = canvasPoint, Modifiers = (int)modifiers };
                try { _componentHandlingMouse.HandleMouseUp(canvasPoint, ctx); } catch { }
                _componentHandlingMouse = null;
                return;
            }

            if (_isDraggingLine && _draggingLine != null)
            {
                _isDraggingLine = false;

                // Try to connect to a new connection point
                var targetPoint = GetConnectionPointAt(canvasPoint);
                if (targetPoint != null && targetPoint != _sourcePoint)
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

                _draggingLine = null;
                _sourcePoint = null;
            }
            else if (_isDragging && _draggingComponent != null)
            {
                _isDragging = false;

                // Handle drop - DrawingManager will handle the ComponentDropped event
                _draggingComponent = null;
            }
            else if (_isDrawingLine)
            {
                _isDrawingLine = false;
                var targetPoint = GetConnectionPointAt(canvasPoint);
                if (targetPoint != null && _sourcePoint.Component != targetPoint.Component && !_sourcePoint.Component.IsConnectedTo(targetPoint.Component))
                {
                    _currentLine.End = targetPoint;
                    _drawingManager.AddLine(_currentLine);
                    _drawingManager.ConnectComponents((SkiaComponent)_sourcePoint.Component, (SkiaComponent)targetPoint.Component);
                }
                _currentLine = null;
            }
            else if (_isSelecting)
            {
                _isSelecting = false;

                // Select components within selection rectangle
                _drawingManager.SelectionManager.SelectComponentsInRect(_selectionRect, _drawingManager.Components, modifiers.HasFlag(SKKeyModifiers.Control));
            }
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
                var ctx = new InteractionContext { MousePosition = canvasPoint, Modifiers = (int)modifiers };
                try { _componentHandlingMouse.HandleMouseMove(canvasPoint, ctx); } catch { }
                return;
            }

            if (_isDraggingLine && _draggingLine != null)
            {
                // Update line end point for visual feedback
                _draggingLine.EndPoint = canvasPoint;
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
            else if (_isDrawingLine && _currentLine != null)
            {
                _currentLine.EndPoint = canvasPoint;
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
        private string GetResizeHandleAt(SkiaComponent component, SKPoint point) => _drawingManager.GetResizeHandleAt(component, point);
        private void StartDrawingLine(IConnectionPoint sourcePoint, SKPoint point) => _drawingManager.StartDrawingLine(sourcePoint, point);
    }
}
