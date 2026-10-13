using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>Opt-in CPU scopes. Attempts count draws; skipped attempts count known noncontributors. Neither measures GPU work.</summary>
    public sealed class SceneRenderProfile
    {
        public double TotalMs { get; internal set; }
        public double PreparationMs { get; internal set; }
        public double GridMs { get; internal set; }
        public double ConnectionsMs { get; internal set; }
        public double AdornersMs { get; internal set; }
        public double StaticMs { get; internal set; }
        public System.Collections.Generic.Dictionary<string, SceneRenderGroup> Groups { get; } = new();
        internal void Record(SkiaComponent component, double milliseconds)
        {
            var name = component is Components.SketchShape sketch ? "Sketch." + sketch.Kind : component.GetType().FullName;
            if (!Groups.TryGetValue(name, out var group)) Groups[name] = group = new SceneRenderGroup();
            group.Attempts++; group.Milliseconds += milliseconds;
        }
        internal void Skip(SkiaComponent component)
        {
            var name = component is Components.SketchShape sketch ? "Sketch." + sketch.Kind : component.GetType().FullName;
            if (!Groups.TryGetValue(name, out var group)) Groups[name] = group = new SceneRenderGroup();
            group.SkippedAttempts++;
        }
    }
    public sealed class SceneRenderGroup
    {
        public int Attempts { get; internal set; }
        public int SkippedAttempts { get; internal set; }
        public double Milliseconds { get; internal set; }
    }
    /// <summary>
    /// Helper class for rendering operations in the drawing manager.
    /// </summary>
    public class RenderingHelper
    {
        private readonly DrawingManager _drawingManager;

        // Reused across frames: the render loop runs at interactive rates, so per-frame
        // list/LINQ/context allocations are avoided on the hot path.
        private readonly System.Collections.Generic.List<SkiaComponent> _dynamicComponents = new System.Collections.Generic.List<SkiaComponent>();
        private readonly System.Collections.Generic.List<SkiaComponent> _staticComponents = new System.Collections.Generic.List<SkiaComponent>();
        private readonly DrawingContext _preContext = new DrawingContext();
        private readonly DrawingContext _drawingContext = new DrawingContext();
        private readonly DrawingContext _staticContext = new DrawingContext();
        /// <summary>Optional synchronous CPU profiling callback. Ordinary drawing does not allocate a profile or read the clock.</summary>
        public Action<SceneRenderProfile> FrameMeasured { get; set; }
        private static double ElapsedMs(long started) => (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;

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
            var zoom = _drawingManager.Zoom;
            var spacing = _drawingManager.GridSpacing;
            if (!float.IsFinite(zoom) || zoom <= 0 || !float.IsFinite(spacing) || spacing <= 0) return;
            // Keep drawn lines at least 8 pixels apart when zoomed out; dense grids cost time and add no guidance.
            while (spacing * zoom < 8) spacing *= 2;

            using var paint = new SKPaint
            {
                Color = ThemeManager.Current.GridColor,
                StrokeWidth = 1 / zoom,
                Style = SKPaintStyle.Stroke
            };

            // The canvas already carries the pan/zoom transform, so its local clip is the visible world area.
            var bounds = canvas.LocalClipBounds;
            var startX = MathF.Floor(bounds.Left / spacing) * spacing;
            var startY = MathF.Floor(bounds.Top / spacing) * spacing;

            for (var x = startX; x <= bounds.Right; x += spacing)
            {
                canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, paint);
            }

            for (var y = startY; y <= bounds.Bottom; y += spacing)
            {
                canvas.DrawLine(bounds.Left, y, bounds.Right, y, paint);
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
            if (!_drawingManager.ShowSelectionHandles) return;
            // Show handles/highlights if ANY selection exists (components, lines, or connection points)
            bool anySelection =
                _drawingManager.SelectionManager.SelectedComponents.Count > 0 ||
                _drawingManager.SelectionManager.SelectedLines.Count > 0 ||
                _drawingManager.SelectionManager.SelectedConnectionPoints.Count > 0;
            if (!anySelection) return;

            using var handlePaint = new SKPaint
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
            DrawAll(canvas,
                applyPanZoom: true,
                includeStatic: true,
                includeGrid: true,
                includeSelection: true,
                zoom: _drawingManager.Zoom,
                pan: _drawingManager.PanOffset);
        }

        /// <summary>Whether the host shows the canvas with inverted lightness (see <see cref="DisplayFilters"/>). It changes how
        /// pictures are drawn on screen only; exports never use it.</summary>
        public bool InvertedDisplay { get; set; }

        /// <summary>
        /// Draws the diagram for export: no pan/zoom transform, no grid, no selection
        /// adorners, and no static screen-space overlays (palette, property editor).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        public void DrawForExport(SKCanvas canvas)
        {
            DrawAll(canvas,
                applyPanZoom: false,
                includeStatic: false,
                includeGrid: false,
                includeSelection: false,
                zoom: 1f,
                pan: new SKPoint(0, 0));
        }

        /// <summary>Renders through the same scene path with host-owned retained pixels.
        /// Overlay ink is in host/CSS coordinates, before the incoming device matrix; null means unknown.
        /// Unknown scene effects or unsupported raster/clip/matrix contexts require ordinary full repaint.</summary>
        public void DrawFrame(SKSurface target, SKColor background, SceneFrameCache cache, Action<SKCanvas> overlay = null,
            Func<SKRect?> overlayInk = null, Action<double> sceneMeasured = null)
        {
            if (target == null || cache == null) throw new ArgumentNullException(target == null ? nameof(target) : nameof(cache));
            var started = sceneMeasured == null ? 0 : Stopwatch.GetTimestamp();
            var canvas = target.Canvas;
            using var pixels = target.PeekPixels();
            var info = pixels?.Info ?? new SKImageInfo(canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height);
            var matrix = canvas.TotalMatrix;
            SKRect? bounds;
            try { bounds = overlay == null ? SKRect.Empty : overlayInk?.Invoke(); }
            catch { cache.Reset(); throw; }
            var supported = pixels != null && info.BytesPerPixel == 4 && canvas.IsClipRect
                && canvas.DeviceClipBounds == new SKRectI(0, 0, info.Width, info.Height)
                && matrix.ScaleX > 0 && matrix.ScaleY > 0 && matrix.SkewX == 0 && matrix.SkewY == 0
                && matrix.Persp0 == 0 && matrix.Persp1 == 0 && matrix.Persp2 == 1
                && float.IsFinite(matrix.ScaleX) && float.IsFinite(matrix.ScaleY) && float.IsFinite(matrix.TransX) && float.IsFinite(matrix.TransY)
                && bounds.HasValue && SceneFrameCache.Finite(bounds.Value)
                && _drawingManager.Lines.Count == 0 && _drawingManager.WorldOverlay == null
                && !_drawingManager.InteractionHelper.IsDrawingLine && !_drawingManager.InteractionHelper.IsSelecting
                && !_drawingManager.ShowSelectionHandles;
            if (supported) foreach (var component in _drawingManager.Components)
                if (component is not (Components.SketchShape or IRetainedVisual) || component.IsStatic || component.Children.Count != 0) { supported = false; break; }
            try
            {
                if (!supported)
                {
                    cache.Uncached(info); canvas.Clear(background); DrawAll(canvas);
                    if (sceneMeasured != null) sceneMeasured(ElapsedMs(started));
                    overlay?.Invoke(canvas); return;
                }
                cache.Begin(_drawingManager, info, matrix, background, bounds.Value);
                using var scratch = SKSurface.Create(info, cache.Scratch.GetPixels(), cache.Scratch.RowBytes);
                var drawing = scratch.Canvas; drawing.SetMatrix(matrix);
                for (var i = 1; i < canvas.SaveCount; i++) drawing.Save();
                drawing.Clear(background);
                DrawAll(drawing, true, true, true, true, _drawingManager.Zoom, _drawingManager.PanOffset, cache);
                if (sceneMeasured != null) sceneMeasured(ElapsedMs(started));
                overlay?.Invoke(drawing); cache.Present(pixels);
            }
            catch { cache.Reset(); throw; }
        }

        private void DrawAll(SKCanvas canvas, bool applyPanZoom, bool includeStatic, bool includeGrid, bool includeSelection, float zoom, SKPoint pan, SceneFrameCache frame = null)
        {
            var measured = FrameMeasured;
            var profile = measured == null ? null : new SceneRenderProfile();
            var frameStart = profile == null ? 0 : Stopwatch.GetTimestamp();
            canvas.Save();

            // Ensure components have up-to-date Bounds and state before drawing, and partition
            // them into dynamic (world space) vs static (screen space overlays) in one pass.
            _preContext.PanOffset = pan;
            _preContext.Zoom = zoom;
            _preContext.Bounds = new SKRect(0, 0, canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height);

            _dynamicComponents.Clear();
            _staticComponents.Clear();

            var components = _drawingManager.Components;
            for (var i = 0; i < components.Count; i++)
            {
                var comp = components[i];
                if (comp == null) continue;

                try
                {
                    comp.Update(_preContext);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Rendering] Update error for {comp.GetType().Name}: {ex.Message}");
                }

                if (comp.IsStatic) _staticComponents.Add(comp);
                else _dynamicComponents.Add(comp);
            }

            var staticComponents = _staticComponents;
            frame?.PrepareAfterUpdate(components, _drawingManager.AnimationTime);
            if (profile != null) profile.PreparationMs = ElapsedMs(frameStart);

            // Apply world transform for dynamic content
            if (applyPanZoom)
            {
                canvas.Translate(pan.X, pan.Y);
                canvas.Scale(zoom);
            }

            // Draw grid if enabled (grid lives in world space)
            var scopeStart = profile == null ? 0 : Stopwatch.GetTimestamp();
            if (includeGrid)
                DrawGrid(canvas);
            if (profile != null) profile.GridMs = ElapsedMs(scopeStart);

            // Create a single, correct context to be passed to all components
            _drawingContext.PanOffset = pan;
            _drawingContext.Zoom = zoom;
            _drawingContext.IncludeSelection = includeSelection;
            _drawingContext.InvertedDisplay = applyPanZoom && InvertedDisplay; // The screen, never an export.
            _drawingContext.AnimationTime = _drawingManager.AnimationTime;
            // Components use world coordinates. Respect the active pan/zoom or export transform.
            _drawingContext.Bounds = canvas.LocalClipBounds;
            var drawingContext = _drawingContext;

            // Draw dynamic (world-space) components only
            foreach (var component in _dynamicComponents)
            {
                if (frame != null && !frame.Contributes(component))
                { frame.LastSkippedComponents++; profile?.Skip(component); continue; }
                if (frame != null) frame.LastDrawnComponents++;
                scopeStart = profile == null ? 0 : Stopwatch.GetTimestamp();
                var clipped = component.ClipBounds is { } clip;
                if (clipped) { canvas.Save(); canvas.ClipRect(component.ClipBounds.Value); }
                try { component.Draw(canvas, drawingContext); } catch { }
                if (clipped) canvas.Restore();
                if (profile != null) profile.Record(component, ElapsedMs(scopeStart));
            }

            // Draw connection lines
            scopeStart = profile == null ? 0 : Stopwatch.GetTimestamp();
            foreach (var line in _drawingManager.Lines)
            {
                if (line is ConnectionLine hidden && !hidden.IsVisible) continue;
                DrawConnectionLineWithZoom(canvas, line, zoom, includeSelection: includeSelection);
            }

            // Draw current line being drawn
            if (_drawingManager.InteractionHelper.IsDrawingLine && _drawingManager.CurrentLine != null)
            {
                DrawConnectionLineWithZoom(canvas, _drawingManager.CurrentLine, zoom, isPreview: true, includeSelection: includeSelection);
            }
            if (profile != null) profile.ConnectionsMs = ElapsedMs(scopeStart);

            scopeStart = profile == null ? 0 : Stopwatch.GetTimestamp();
            if (includeSelection)
            {
                // Draw selection rectangle
                if (_drawingManager.InteractionHelper.IsSelecting)
                {
                    DrawSelectionRectangle(canvas, _drawingManager.InteractionHelper.SelectionRect);
                }

                // Draw selection handles for selected components
                DrawSelectionHandles(canvas);

                // Draw host-provided world-space annotations (e.g. comment pins)
                if (_drawingManager.WorldOverlay != null)
                {
                    try { _drawingManager.WorldOverlay(canvas); } catch { }
                }
            }
            if (profile != null) profile.AdornersMs = ElapsedMs(scopeStart);

            // Restore the canvas state
            scopeStart = profile == null ? 0 : Stopwatch.GetTimestamp();
            canvas.Restore();

            // Draw static (screen-space) overlay components without world transforms
            if (includeStatic && staticComponents.Count > 0)
            {
                _staticContext.PanOffset = new SKPoint(0, 0);
                _staticContext.Zoom = 1f;
                _staticContext.Bounds = canvas.DeviceClipBounds;

                foreach (var component in staticComponents)
                {
                    try { component.Draw(canvas, _staticContext); } catch { }
                }
            }
            if (profile != null)
            {
                profile.StaticMs = ElapsedMs(scopeStart);
                profile.TotalMs = ElapsedMs(frameStart);
                measured(profile);
            }
        }

        /// <summary>
        /// Draw a connection line while adapting visual properties to current zoom and flow settings. An export passes
        /// includeSelection false, so a selected line draws no handles there.
        /// </summary>
        private void DrawConnectionLineWithZoom(SKCanvas canvas, IConnectionLine line, float zoom, bool isPreview = false, bool includeSelection = true)
        {
            if (line == null)
                return;

            zoom = Math.Max(0.0001f, zoom);

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
                if (line is ConnectionLine timed) timed.AnimationTime = _drawingManager.AnimationTime;
                if (_drawingManager.ConnectorsKeepScreenSize)
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

                // If selected, draw endpoint handles to indicate editability, unless the host draws its own or this is an export
                if (line.IsSelected && includeSelection && _drawingManager.ShowSelectionHandles)
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
