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
        private readonly DrawingManager _drawingManager;

        /// <summary>
        /// Gets the currently selected components.
        /// </summary>
        public IReadOnlyList<SkiaComponent> SelectedComponents => _selectedComponents;

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
            foreach (var component in _selectedComponents)
            {
                component.IsSelected = false;
            }
            _selectedComponents.Clear();
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
            }
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
        /// Gets a value indicating whether a component is selected.
        /// </summary>
        /// <param name="component">The component to test.</param>
        /// <returns>true if the component is selected; otherwise, false.</returns>
        public bool IsSelected(SkiaComponent component)
        {
            return _selectedComponents.Contains(component);
        }

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
