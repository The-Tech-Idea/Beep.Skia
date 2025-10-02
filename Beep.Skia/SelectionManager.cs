using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Helper class for managing component selection in the drawing manager.
    /// </summary>
    public class SelectionManager
    {
        private readonly List<SkiaComponent> _selectedComponents;
        private readonly List<IConnectionLine> _selectedLines;
        private readonly List<IConnectionPoint> _selectedConnectionPoints;
        private readonly DrawingManager _drawingManager;

        /// <summary>
        /// Gets the currently selected components.
        /// </summary>
    public IReadOnlyList<SkiaComponent> SelectedComponents => _selectedComponents;
    public IReadOnlyList<IConnectionLine> SelectedLines => _selectedLines;
    public IReadOnlyList<IConnectionPoint> SelectedConnectionPoints => _selectedConnectionPoints;

        /// <summary>
        /// Occurs when the selection changes.
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionManager"/> class.
        /// </summary>
        /// <param name="drawingManager">The drawing manager that owns this selection manager.</param>
        public SelectionManager(DrawingManager drawingManager)
        {
            _drawingManager = drawingManager;
            _selectedComponents = new List<SkiaComponent>();
            _selectedLines = new List<IConnectionLine>();
            _selectedConnectionPoints = new List<IConnectionPoint>();
        }

        /// <summary>
        /// Selects a component.
        /// </summary>
        /// <param name="component">The component to select.</param>
        /// <param name="addToSelection">Whether to add to existing selection.</param>
        public void SelectComponent(SkiaComponent component, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                ClearSelection();
            }

            if (component != null && !_selectedComponents.Contains(component))
            {
                _selectedComponents.Add(component);
                component.IsSelected = true;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearSelection()
        {
            // Clear component selection
            foreach (var component in _selectedComponents)
            {
                component.IsSelected = false;
            }
            _selectedComponents.Clear();

            // Clear line selection
            foreach (var line in _selectedLines)
            {
                if (line != null) line.IsSelected = false;
            }
            _selectedLines.Clear();

            // Clear connection point selection
            _selectedConnectionPoints.Clear();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Selects all components within the specified rectangle.
        /// </summary>
        /// <param name="selectionRect">The selection rectangle.</param>
        /// <param name="components">The components to test against.</param>
        /// <param name="addToSelection">Whether to add to existing selection.</param>
        public void SelectComponentsInRect(SKRect selectionRect, IEnumerable<SkiaComponent> components, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                ClearSelection();
            }

            foreach (var component in components)
            {
                if (selectionRect.IntersectsWith(component.Bounds))
                {
                    SelectComponent(component, true);
                }

                // Select connection points of this component contained in the rectangle
                foreach (var p in component.InConnectionPoints)
                {
                    if (ConnectionPointInRect(p, selectionRect))
                    {
                        SelectConnectionPoint(p, true);
                    }
                }
                foreach (var p in component.OutConnectionPoints)
                {
                    if (ConnectionPointInRect(p, selectionRect))
                    {
                        SelectConnectionPoint(p, true);
                    }
                }
            }

            // Select lines intersecting the selection rectangle
            foreach (var line in _drawingManager.Lines)
            {
                if (line?.Start == null || line.End == null) continue;
                if (LineIntersectsRect(line, selectionRect))
                {
                    SelectLine(line, true);
                }
            }
        }

        private static bool ConnectionPointInRect(IConnectionPoint p, SKRect rect)
        {
            if (p == null) return false;
            // Prefer Bounds if provided; else test center/position
            if (p is ConnectionPoint cp && cp.Bounds.Width > 0 && cp.Bounds.Height > 0)
                return rect.IntersectsWith(cp.Bounds);
            var center = p.Position;
            return rect.Contains(center);
        }

        private static bool LineIntersectsRect(IConnectionLine line, SKRect rect)
        {
            var a = line.Start != null ? line.Start.Position : line.EndPoint;
            var b = line.End != null ? line.End.Position : line.EndPoint;

            // If either endpoint is inside the rect, count it
            if (rect.Contains(a) || rect.Contains(b)) return true;

            // Check according to routing
            if (line.RoutingMode == LineRoutingMode.Orthogonal)
            {
                float midX = (a.X + b.X) * 0.5f;
                var p2 = new SKPoint(midX, a.Y);
                var p3 = new SKPoint(midX, b.Y);
                return SegmentIntersectsRect(a, p2, rect)
                    || SegmentIntersectsRect(p2, p3, rect)
                    || SegmentIntersectsRect(p3, b, rect);
            }
            else if (line.RoutingMode == LineRoutingMode.Curved)
            {
                // Approximate: sample points along a cubic curve and check if any segment intersects
                var c1 = new SKPoint((a.X * 2 + b.X) / 3f, a.Y);
                var c2 = new SKPoint((b.X * 2 + a.X) / 3f, b.Y);
                const int steps = 12;
                SKPoint prev = a;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    var p = BezierPoint(a, c1, c2, b, t);
                    if (SegmentIntersectsRect(prev, p, rect)) return true;
                    prev = p;
                }
                return false;
            }
            else
            {
                return SegmentIntersectsRect(a, b, rect);
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

        private static bool SegmentIntersectsRect(SKPoint p1, SKPoint p2, SKRect r)
        {
            // Quick reject using bounding boxes
            var minX = Math.Min(p1.X, p2.X);
            var maxX = Math.Max(p1.X, p2.X);
            var minY = Math.Min(p1.Y, p2.Y);
            var maxY = Math.Max(p1.Y, p2.Y);
            if (maxX < r.Left || minX > r.Right || maxY < r.Top || minY > r.Bottom)
                return false;

            // Check intersection with each rect edge
            var rTL = new SKPoint(r.Left, r.Top);
            var rTR = new SKPoint(r.Right, r.Top);
            var rBR = new SKPoint(r.Right, r.Bottom);
            var rBL = new SKPoint(r.Left, r.Bottom);
            if (SegmentsIntersect(p1, p2, rTL, rTR)) return true;
            if (SegmentsIntersect(p1, p2, rTR, rBR)) return true;
            if (SegmentsIntersect(p1, p2, rBR, rBL)) return true;
            if (SegmentsIntersect(p1, p2, rBL, rTL)) return true;
            return false;
        }

        private static float Cross(SKPoint a, SKPoint b, SKPoint c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static bool SegmentsIntersect(SKPoint p, SKPoint p2, SKPoint q, SKPoint q2)
        {
            float rxs = Cross(p, p2, q2) - Cross(p, p2, q);
            float qpxr = Cross(p, p2, q);
            if (Math.Abs(rxs) < 1e-6)
            {
                // Colinear or parallel - treat as bounding box overlap
                var pr = new SKRect(Math.Min(p.X, p2.X), Math.Min(p.Y, p2.Y), Math.Max(p.X, p2.X), Math.Max(p.Y, p2.Y));
                var qr = new SKRect(Math.Min(q.X, q2.X), Math.Min(q.Y, q2.Y), Math.Max(q.X, q2.X), Math.Max(q.Y, q2.Y));
                return pr.IntersectsWith(qr);
            }
            float t = Cross(q, q2, p) / rxs;
            float u = qpxr / rxs;
            return (t >= 0 && t <= 1 && u >= 0 && u <= 1);
        }

        /// <summary>
        /// Removes a component from the selection.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveFromSelection(SkiaComponent component)
        {
            if (_selectedComponents.Remove(component))
            {
                component.IsSelected = false;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Selects a connection line.
        /// </summary>
        public void SelectLine(IConnectionLine line, bool addToSelection = false)
        {
            if (line == null) return;
            if (!addToSelection)
            {
                ClearSelection();
            }
            if (!_selectedLines.Contains(line))
            {
                _selectedLines.Add(line);
                line.IsSelected = true;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Removes a connection line from the selection.
        /// </summary>
        public void RemoveFromSelection(IConnectionLine line)
        {
            if (line == null) return;
            if (_selectedLines.Remove(line))
            {
                line.IsSelected = false;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Selects a connection point.
        /// </summary>
        public void SelectConnectionPoint(IConnectionPoint point, bool addToSelection = false)
        {
            if (point == null) return;
            if (!addToSelection)
            {
                ClearSelection();
            }
            if (!_selectedConnectionPoints.Contains(point))
            {
                _selectedConnectionPoints.Add(point);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Removes a connection point from the selection.
        /// </summary>
        public void RemoveFromSelection(IConnectionPoint point)
        {
            if (point == null) return;
            if (_selectedConnectionPoints.Remove(point))
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets a value indicating whether a component is selected.
        /// </summary>
        /// <param name="component">The component to test.</param>
        /// <returns>true if the component is selected; otherwise, false.</returns>
        public bool IsSelected(SkiaComponent component)
        {
            return _selectedComponents.Contains(component);
        }

        /// <summary>
        /// Checks if a line is currently selected.
        /// </summary>
        public bool IsSelected(IConnectionLine line) => _selectedLines.Contains(line);

        /// <summary>
        /// Checks if a connection point is currently selected.
        /// </summary>
        public bool IsSelected(IConnectionPoint point) => _selectedConnectionPoints.Contains(point);

        /// <summary>
        /// Gets the number of selected components.
        /// </summary>
    public int SelectionCount => _selectedComponents.Count;

        /// <summary>
        /// Gets the bounds of all selected components combined.
        /// </summary>
        /// <returns>The combined bounds rectangle.</returns>
        public SKRect GetSelectionBounds()
        {
            if (_selectedComponents.Count == 0)
                return SKRect.Empty;

            var bounds = _selectedComponents[0].Bounds;
            for (int i = 1; i < _selectedComponents.Count; i++)
            {
                bounds = SKRect.Union(bounds, _selectedComponents[i].Bounds);
            }
            return bounds;
        }
    }
}
