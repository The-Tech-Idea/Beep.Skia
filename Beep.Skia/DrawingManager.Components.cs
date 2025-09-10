using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia
{
    public partial class DrawingManager
    {
        /// <summary>
        /// Adds a workflow component to the drawing manager.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
        public void AddComponent(SkiaComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component), "Component cannot be null.");
            try
            {
                var preListPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log");
                var snapshot = string.Join(",", _components.Select(c => c.Name + "@" + c.X + "," + c.Y));
                System.IO.File.AppendAllText(preListPath, $"[AddComponent.Pre] Count={_components.Count} Existing=[{snapshot}]\n");
            }
            catch { }
            _components.Add(component);
            // Log the component being added for debugging drop/creation coordinate issues
            try
            {
                var msg = $"[DrawingManager.AddComponent] Added: Type={component.GetType().FullName} Name={component.Name} X={component.X},Y={component.Y},W={component.Width},H={component.Height}";
                try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { }
            }
            catch { }
            try
            {
                var postListPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log");
                var snapshot2 = string.Join(",", _components.Select(c => c.Name + "@" + c.X + "," + c.Y));
                System.IO.File.AppendAllText(postListPath, $"[AddComponent.Post] Count={_components.Count} Existing=[{snapshot2}]\n");
            }
            catch { }
            _historyManager.ExecuteAction(new AddComponentAction(this, component));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Removes a workflow component from the drawing manager.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
        public void RemoveComponent(SkiaComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component), "Component cannot be null.");

            // Remove all connections to/from this component
            var linesToRemove = _lines.Where(line =>
                line.Start?.Component == component || line.End?.Component == component).ToList();

            foreach (var line in linesToRemove)
            {
                _lines.Remove(line);
            }

            _components.Remove(component);
            _selectionManager.RemoveFromSelection(component);
            _historyManager.ExecuteAction(new RemoveComponentAction(this, component, linesToRemove));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Deletes all selected components.
        /// </summary>
        public void DeleteSelectedComponents()
        {
            var componentsToDelete = _selectionManager.SelectedComponents.ToList();
            var linesToDelete = new List<IConnectionLine>();

            foreach (var component in componentsToDelete)
            {
                var componentLines = _lines.Where(line =>
                    line.Start?.Component == component || line.End?.Component == component).ToList();
                linesToDelete.AddRange(componentLines);
            }

            _historyManager.ExecuteAction(new DeleteComponentsAction(this, componentsToDelete, linesToDelete));

            foreach (var line in linesToDelete)
            {
                _lines.Remove(line);
            }

            foreach (var component in componentsToDelete)
            {
                _components.Remove(component);
                _selectionManager.RemoveFromSelection(component);
            }

            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Copies selected components to clipboard.
        /// </summary>
        public void CopySelectedComponents()
        {
            // Implementation for copying components
            // This would typically serialize the selected components
        }

        /// <summary>
        /// Pastes components from clipboard.
        /// </summary>
        /// <param name="position">The position to paste at.</param>
        public void PasteComponents(SKPoint position)
        {
            // Implementation for pasting components
            // This would typically deserialize and place components at the given position
        }

        /// <summary>
        /// Moves selected components by the specified offset.
        /// </summary>
        /// <param name="offset">The offset to move by.</param>
        public void MoveSelectedComponents(SKPoint offset)
        {
            if (_selectionManager.SelectionCount == 0) return;

            var snappedOffset = SnapToGrid ? SnapOffset(offset) : offset;
            _historyManager.ExecuteAction(new MoveComponentsAction(this, _selectionManager.SelectedComponents.ToList(), snappedOffset));

            foreach (var component in _selectionManager.SelectedComponents)
            {
                component.Move(snappedOffset);
            }

            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Removes all components and connection lines from the drawing manager.
        /// This is a safe public API intended for demos and tools.
        /// </summary>
        public void ClearComponents()
        {
            // Remove all lines
            _lines.Clear();

            // Remove all components
            _components.Clear();

            // Clear selection
            try { _selectionManager?.ClearSelection(); } catch { }

            // Notify renderers
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Returns a snapshot of current components.
        /// </summary>
        /// <returns>Read-only list of components.</returns>
        public IReadOnlyList<SkiaComponent> GetComponents() => _components.AsReadOnly();

        /// <summary>
        /// Snaps an offset to the grid.
        /// </summary>
        /// <param name="offset">The offset to snap.</param>
        /// <returns>The snapped offset.</returns>
        private SKPoint SnapOffset(SKPoint offset)
        {
            // For movement, we want to snap the final position, not the offset
            return offset; // Grid snapping for movement is handled differently
        }

        /// <summary>
        /// Gets the component at the specified point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The component at the specified point, or null if no component is found.</returns>
        internal SkiaComponent GetComponentAt(SKPoint point)
        {
            return _components.LastOrDefault(component => component.HitTest(point));
        }
    }
}
