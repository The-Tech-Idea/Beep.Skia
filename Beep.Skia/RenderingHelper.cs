using SkiaSharp;
using System;

namespace Beep.Skia
{
    /// <summary>
    /// Helper class for rendering operations in the drawing manager.
    /// </summary>
    public class RenderingHelper
    {
        private readonly DrawingManager _drawingManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderingHelper"/> class.
        /// </summary>
        /// <param name="drawingManager">The drawing manager that owns this rendering helper.</param>
        public RenderingHelper(DrawingManager drawingManager)
        {
            _drawingManager = drawingManager;
        }

        /// <summary>
        /// Draws the grid on the canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void DrawGrid(SKCanvas canvas)
        {
            if (!_drawingManager.ShowGrid) return;

            var paint = new SKPaint
            {
                Color = new SKColor(200, 200, 200, 100),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            };

            // Get canvas bounds (considering zoom and pan)
            var bounds = canvas.DeviceClipBounds;
            var startX = (float)Math.Ceiling(bounds.Left / (_drawingManager.Zoom * _drawingManager.GridSpacing)) * _drawingManager.GridSpacing;
            var endX = (float)Math.Floor(bounds.Right / (_drawingManager.Zoom * _drawingManager.GridSpacing)) * _drawingManager.GridSpacing;
            var startY = (float)Math.Ceiling(bounds.Top / (_drawingManager.Zoom * _drawingManager.GridSpacing)) * _drawingManager.GridSpacing;
            var endY = (float)Math.Floor(bounds.Bottom / (_drawingManager.Zoom * _drawingManager.GridSpacing)) * _drawingManager.GridSpacing;

            // Draw vertical lines
            for (float x = startX; x <= endX; x += _drawingManager.GridSpacing)
            {
                canvas.DrawLine(x, startY, x, endY, paint);
            }

            // Draw horizontal lines
            for (float y = startY; y <= endY; y += _drawingManager.GridSpacing)
            {
                canvas.DrawLine(startX, y, endX, y, paint);
            }
        }

        /// <summary>
        /// Draws the selection rectangle.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="selectionRect">The selection rectangle to draw.</param>
        public void DrawSelectionRectangle(SKCanvas canvas, SKRect selectionRect)
        {
            var paint = new SKPaint
            {
                Color = new SKColor(100, 149, 237, 100),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            };

            var fillPaint = new SKPaint
            {
                Color = new SKColor(100, 149, 237, 50),
                Style = SKPaintStyle.Fill
            };

            canvas.DrawRect(selectionRect, fillPaint);
            canvas.DrawRect(selectionRect, paint);
        }

        /// <summary>
        /// Draws selection handles for selected components.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void DrawSelectionHandles(SKCanvas canvas)
        {
            if (_drawingManager.SelectionManager.SelectionCount == 0) return;

            var handlePaint = new SKPaint
            {
                Color = SKColors.CornflowerBlue,
                Style = SKPaintStyle.Fill
            };

            var handleSize = 6.0f / _drawingManager.Zoom;

            foreach (var component in _drawingManager.SelectionManager.SelectedComponents)
            {
                var bounds = component.Bounds;

                // Draw resize handles at corners
                var handles = new[]
                {
                    new SKPoint(bounds.Left, bounds.Top),
                    new SKPoint(bounds.Right, bounds.Top),
                    new SKPoint(bounds.Right, bounds.Bottom),
                    new SKPoint(bounds.Left, bounds.Bottom)
                };

                foreach (var handle in handles)
                {
                    canvas.DrawRect(handle.X - handleSize/2, handle.Y - handleSize/2, handleSize, handleSize, handlePaint);
                }
            }
        }

        /// <summary>
        /// Draws all components and connection lines on the canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void DrawAll(SKCanvas canvas)
        {
            // Save the canvas state
            canvas.Save();

            // Apply zoom and pan transformations
            canvas.Translate(_drawingManager.PanOffset);
            canvas.Scale(_drawingManager.Zoom);

            // Draw grid if enabled
            DrawGrid(canvas);

            // Draw components
            foreach (var component in _drawingManager.Components)
            {
                component.Draw(canvas);
            }

            // Draw connection lines
            foreach (var line in _drawingManager.Lines)
            {
                line.Draw(canvas);
            }

            // Draw current line being drawn
            if (_drawingManager.InteractionHelper.IsDrawingLine && _drawingManager.CurrentLine != null)
            {
                _drawingManager.CurrentLine.Draw(canvas);
            }

            // Draw selection rectangle
            if (_drawingManager.InteractionHelper.IsSelecting)
            {
                DrawSelectionRectangle(canvas, _drawingManager.InteractionHelper.SelectionRect);
            }

            // Draw selection handles for selected components
            DrawSelectionHandles(canvas);

            // Restore the canvas state
            canvas.Restore();
        }

        /// <summary>
        /// Gets the resize handle at the specified point on a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The handle identifier, or null if no handle is at the point.</returns>
        public string GetResizeHandleAt(SkiaComponent component, SKPoint point)
        {
            var bounds = component.Bounds;
            var handleSize = 6.0f / _drawingManager.Zoom;

            var handles = new Dictionary<string, SKPoint>
            {
                ["top-left"] = new SKPoint(bounds.Left, bounds.Top),
                ["top-right"] = new SKPoint(bounds.Right, bounds.Top),
                ["bottom-right"] = new SKPoint(bounds.Right, bounds.Bottom),
                ["bottom-left"] = new SKPoint(bounds.Left, bounds.Bottom)
            };

            foreach (var handle in handles)
            {
                var handleRect = new SKRect(
                    handle.Value.X - handleSize/2,
                    handle.Value.Y - handleSize/2,
                    handle.Value.X + handleSize/2,
                    handle.Value.Y + handleSize/2
                );

                if (handleRect.Contains(point))
                {
                    return handle.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the arrow type ("start" or "end") at the specified point on a line.
        /// </summary>
        /// <param name="line">The connection line.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>"start", "end", or null if no arrow is at the point.</returns>
        public string GetArrowAt(IConnectionLine line, SKPoint point)
        {
            if (line.ArrowContainsPoint(point, true)) return "start";
            if (line.ArrowContainsPoint(point, false)) return "end";
            return null;
        }
    }
}
