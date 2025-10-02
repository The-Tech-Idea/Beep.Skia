using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using Beep.Skia.Model;
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
            // Show handles/highlights if ANY selection exists (components, lines, or connection points)
            bool anySelection =
                _drawingManager.SelectionManager.SelectedComponents.Count > 0 ||
                _drawingManager.SelectionManager.SelectedLines.Count > 0 ||
                _drawingManager.SelectionManager.SelectedConnectionPoints.Count > 0;
            if (!anySelection) return;

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

            // Highlight selected connection points if any
            if (_drawingManager.SelectionManager.SelectedConnectionPoints.Count > 0)
            {
                using var cpPaint = new SKPaint { Color = SKColors.OrangeRed, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1f, 2f / _drawingManager.Zoom), IsAntialias = true };
                foreach (var cp in _drawingManager.SelectionManager.SelectedConnectionPoints)
                {
                    var center = cp.Position;
                    float r = Math.Max(3f, 6f / _drawingManager.Zoom);
                    canvas.DrawCircle(center, r, cpPaint);
                }
            }
        }

        /// <summary>
        /// Draws all components and connection lines on the canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void DrawAll(SKCanvas canvas)
        {
            canvas.Save();

            // Ensure components have up-to-date Bounds and state before drawing.
            var preContext = new DrawingContext
            {
                PanOffset = _drawingManager.PanOffset,
                Zoom = _drawingManager.Zoom,
                Bounds = new SKRect(0, 0, canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height)
            };

            // Update all components once per frame
            foreach (var comp in _drawingManager.Components.ToList())
            {
                try
                {
                    // Log before and after calling Update to determine if Update is executed or throws
                        try
                        {
                            var updateLog = Path.Combine(Path.GetTempPath(), "beepskia_update.log");
                            File.AppendAllText(updateLog, $"[Rendering.PreUpdate] {DateTime.UtcNow:o} Calling Update on {comp.GetType().FullName} State={comp.State}\\n");
                            var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                            File.AppendAllText(logPath, $"[Rendering.PreUpdate] Calling Update on {comp.GetType().FullName}\\n");
                        }
                        catch { }

                        comp.Update(preContext);

                        try
                        {
                            var updateLog = Path.Combine(Path.GetTempPath(), "beepskia_update.log");
                            File.AppendAllText(updateLog, $"[Rendering.PostUpdate] {DateTime.UtcNow:o} Completed Update on {comp.GetType().FullName} State={comp.State}\\n");
                            var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                            File.AppendAllText(logPath, $"[Rendering.PostUpdate] Completed Update on {comp.GetType().FullName}\\n");
                        }
                        catch { }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Rendering] Pre-update error for {comp?.GetType().Name}: {ex.Message}");
                }
            }

            // Partition components into dynamic (world space) vs static (screen space overlays)
            var dynamicComponents = _drawingManager.Components.Where(c => !(c?.IsStatic ?? false)).ToList();
            var staticComponents = _drawingManager.Components.Where(c => (c?.IsStatic ?? false)).ToList();

            // Apply world transform for dynamic content
            canvas.Translate(_drawingManager.PanOffset.X, _drawingManager.PanOffset.Y);
            canvas.Scale(_drawingManager.Zoom);

            // Diagnostic logging: report transform and component bounds (helpful to debug invisible palette)
            try
            {
                var header = $"[Rendering] PanOffset={_drawingManager.PanOffset}, Zoom={_drawingManager.Zoom}, Components={_drawingManager.Components.Count}";
                Debug.WriteLine(header);
                Console.WriteLine(header);
                try
                {
                    var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                    File.AppendAllText(logPath, header + Environment.NewLine);
                }
                catch { }

                foreach (var c in _drawingManager.Components)
                {
                    try
                    {
                        var line = $"[Rendering] Component: Type={c.GetType().FullName}, X={c.X}, Y={c.Y}, W={c.Width}, H={c.Height}, Bounds={c.Bounds}";
                        Debug.WriteLine(line);
                        Console.WriteLine(line);
                        try
                        {
                            var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                            File.AppendAllText(logPath, line + Environment.NewLine);
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch { }

            // Draw grid if enabled (grid lives in world space)
            DrawGrid(canvas);

            // Create a single, correct context to be passed to all components
            var drawingContext = new DrawingContext
            {
                PanOffset = _drawingManager.PanOffset,
                Zoom = _drawingManager.Zoom,
                Bounds = canvas.DeviceClipBounds
            };

            // Draw dynamic (world-space) components only
            foreach (var component in dynamicComponents)
            {
                component.Draw(canvas, drawingContext);
            }

            // Draw connection lines
            foreach (var line in _drawingManager.Lines)
            {
                DrawConnectionLineWithZoom(canvas, line);
            }

            // Draw current line being drawn
            if (_drawingManager.InteractionHelper.IsDrawingLine && _drawingManager.CurrentLine != null)
            {
                DrawConnectionLineWithZoom(canvas, _drawingManager.CurrentLine, isPreview: true);
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

            // Draw static (screen-space) overlay components without world transforms
            if (staticComponents.Count > 0)
            {
                var uiContext = new DrawingContext
                {
                    PanOffset = new SKPoint(0, 0),
                    Zoom = 1f,
                    Bounds = canvas.DeviceClipBounds
                };
                foreach (var component in staticComponents)
                {
                    try { component.Draw(canvas, uiContext); } catch { }
                }
            }
        }

        /// <summary>
        /// Draw a connection line while adapting visual properties to current zoom and flow settings.
        /// </summary>
        private void DrawConnectionLineWithZoom(SKCanvas canvas, IConnectionLine line, bool isPreview = false)
        {
            if (line == null)
                return;

            float zoom = Math.Max(0.0001f, _drawingManager.Zoom);

            // Ensure paint exists
            line.Paint ??= new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true, StrokeCap = SKStrokeCap.Round };

            // Stash original values to restore after drawing
            float origStroke = line.Paint.StrokeWidth;
            float origArrow = line.ArrowSize;
            var origDash = line.DashPattern;
            bool origShowStart = line.ShowStartArrow;
            bool origShowEnd = line.ShowEndArrow;

            try
            {
                // Keep visual thickness/device-space similar across zoom levels
                line.Paint.StrokeWidth = Math.Max(1f, origStroke / zoom);

                // Keep arrow heads readable across zoom
                line.ArrowSize = Math.Max(6f, origArrow / zoom);

                // Scale dash pattern to device space
                if (origDash != null && origDash.Length >= 2)
                {
                    var scaled = new float[origDash.Length];
                    for (int i = 0; i < origDash.Length; i++)
                        scaled[i] = Math.Max(1f, origDash[i] / zoom);
                    line.DashPattern = scaled;
                }

                // Derive arrow visibility from flow direction if specified
                switch (line.FlowDirection)
                {
                    case DataFlowDirection.None:
                        // Respect explicit arrow flags as-is
                        break;
                    case DataFlowDirection.Forward:
                        line.ShowStartArrow = false;
                        line.ShowEndArrow = true;
                        break;
                    case DataFlowDirection.Backward:
                        line.ShowStartArrow = true;
                        line.ShowEndArrow = false;
                        break;
                    case DataFlowDirection.Bidirectional:
                        line.ShowStartArrow = true;
                        line.ShowEndArrow = true;
                        break;
                }

                // Apply a subtle preview style when dragging a new line
                if (isPreview)
                {
                    if (line.DashPattern == null)
                    {
                        line.DashPattern = new[] { 8f / zoom, 8f / zoom };
                    }
                }

                // Draw the line using its own advanced rendering
                line.Draw(canvas);

                // If selected, draw endpoint handles to indicate editability
                if (line.IsSelected)
                {
                    // Use available endpoints; when End is null (preview), use EndPoint
                    var a = line.Start != null ? line.Start.Position : line.EndPoint;
                    var b = line.End != null ? line.End.Position : line.EndPoint;
                    DrawLineEndpointHandles(canvas, a, b, zoom);
                }
            }
            finally
            {
                // Restore original properties to avoid side effects
                line.Paint.StrokeWidth = origStroke;
                line.ArrowSize = origArrow;
                line.DashPattern = origDash;
                line.ShowStartArrow = origShowStart;
                line.ShowEndArrow = origShowEnd;
            }
        }

        private void DrawLineEndpointHandles(SKCanvas canvas, SKPoint start, SKPoint end, float zoom)
        {
            float size = 6f / zoom;
            using var handlePaint = new SKPaint { Color = SKColors.CornflowerBlue, Style = SKPaintStyle.Fill, IsAntialias = true };

            // Start handle
            canvas.DrawRect(start.X - size / 2, start.Y - size / 2, size, size, handlePaint);
            // End handle
            canvas.DrawRect(end.X - size / 2, end.Y - size / 2, size, size, handlePaint);
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
            if (line == null)
                return null;

            // Use zoom-aware arrow hit-testing to match drawn size
            float zoom = Math.Max(0.0001f, _drawingManager.Zoom);
            float arrow = Math.Max(6f, line.ArrowSize / zoom);

            // Local helper to compute bounding rect of an arrowhead
            static SKRect ArrowBounds(SKPoint tip, SKPoint tail, float size)
            {
                var angle = (float)Math.Atan2(tip.Y - tail.Y, tip.X - tail.X);
                var p1 = new SKPoint(
                    tip.X - size * (float)Math.Cos(angle + Math.PI / 6),
                    tip.Y - size * (float)Math.Sin(angle + Math.PI / 6));
                var p2 = new SKPoint(
                    tip.X - size * (float)Math.Cos(angle - Math.PI / 6),
                    tip.Y - size * (float)Math.Sin(angle - Math.PI / 6));
                var left = Math.Min(tip.X, Math.Min(p1.X, p2.X));
                var top = Math.Min(tip.Y, Math.Min(p1.Y, p2.Y));
                var right = Math.Max(tip.X, Math.Max(p1.X, p2.X));
                var bottom = Math.Max(tip.Y, Math.Max(p1.Y, p2.Y));
                return new SKRect(left, top, right, bottom);
            }

            var hasStart = line.Start != null || (line.EndPoint != default);
            var hasEnd = line.End != null || (line.EndPoint != default);

            if (line.ShowStartArrow && hasStart)
            {
                var tip = (line.Start != null ? line.Start.Position : line.EndPoint);
                var tail = (line.End != null ? line.End.Position : line.EndPoint);
                var r = ArrowBounds(tip, tail, arrow);
                if (r.Contains(point)) return "start";
            }
            if (line.ShowEndArrow && hasEnd)
            {
                var tip = (line.End != null ? line.End.Position : line.EndPoint);
                var tail = (line.Start != null ? line.Start.Position : line.EndPoint);
                var r = ArrowBounds(tip, tail, arrow);
                if (r.Contains(point)) return "end";
            }
            return null;
        }

        /// <summary>
        /// Zoom-aware line hit-testing supporting straight, orthogonal, and curved routing.
        /// </summary>
        public bool LineContainsPoint(IConnectionLine line, SKPoint point)
        {
            if (line == null)
                return false;
            var a = line.Start != null ? line.Start.Position : line.EndPoint;
            var b = line.End != null ? line.End.Position : line.EndPoint;
            float zoom = Math.Max(0.0001f, _drawingManager.Zoom);
            // Tolerance ~ 5 device px plus half stroke width, mapped into canvas space
            float tol = Math.Max(3f, 5f / zoom);
            if (line.Paint != null)
            {
                tol = Math.Max(tol, (line.Paint.StrokeWidth * 0.5f) / zoom);
            }

            // Basic distance-to-segment helper
            static float DistPointToSeg(SKPoint p, SKPoint s, SKPoint e)
            {
                float dx = e.X - s.X;
                float dy = e.Y - s.Y;
                float len2 = dx * dx + dy * dy;
                if (len2 <= 1e-6f) return (float)Math.Sqrt((p.X - s.X) * (p.X - s.X) + (p.Y - s.Y) * (p.Y - s.Y));
                float t = ((p.X - s.X) * dx + (p.Y - s.Y) * dy) / len2;
                t = Math.Max(0, Math.Min(1, t));
                var proj = new SKPoint(s.X + t * dx, s.Y + t * dy);
                float px = p.X - proj.X;
                float py = p.Y - proj.Y;
                return (float)Math.Sqrt(px * px + py * py);
            }

            switch (line.RoutingMode)
            {
                case LineRoutingMode.Orthogonal:
                {
                    float midX = (a.X + b.X) * 0.5f;
                    var p2 = new SKPoint(midX, a.Y);
                    var p3 = new SKPoint(midX, b.Y);
                    if (DistPointToSeg(point, a, p2) <= tol) return true;
                    if (DistPointToSeg(point, p2, p3) <= tol) return true;
                    if (DistPointToSeg(point, p3, b) <= tol) return true;
                    return false;
                }
                case LineRoutingMode.Curved:
                {
                    // Same control points used in drawing
                    var c1 = new SKPoint((a.X * 2 + b.X) / 3f, a.Y);
                    var c2 = new SKPoint((b.X * 2 + a.X) / 3f, b.Y);
                    // Adaptive sampling density: more samples when zoomed out or the span is long
                    float span = (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                    int steps = Math.Clamp((int)(span / (10f * zoom)), 16, 128);
                    SKPoint prev = a;
                    for (int i = 1; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        var p = BezierPoint(a, c1, c2, b, t);
                        if (DistPointToSeg(point, prev, p) <= tol) return true;
                        prev = p;
                    }
                    return false;
                }
                default:
                {
                    return DistPointToSeg(point, a, b) <= tol;
                }
            }
        }

        private static SKPoint BezierPoint(SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            var p = new SKPoint();
            p.X = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
            p.Y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;
            return p;
        }
    }
}
