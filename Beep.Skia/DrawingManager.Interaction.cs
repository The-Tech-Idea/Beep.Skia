using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia
{
    public partial class DrawingManager
    {
        /// <summary>
        /// Handles mouse down events for component interaction and connection creation.
        /// </summary>
        /// <param name="point">The point where the mouse down occurred.</param>
        /// <param name="modifiers">Keyboard modifiers (Ctrl, Shift, Alt).</param>
        /// <param name="mouseButton">Mouse button pressed (0=left, 1=right, 2=middle).</param>
        public void HandleMouseDown(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None, int mouseButton = 0)
        {
            _interactionHelper.HandleMouseDown(point, modifiers, mouseButton);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Handles mouse up events to complete dragging or connection operations.
        /// </summary>
        /// <param name="point">The point where the mouse up occurred.</param>
        /// <param name="modifiers">Keyboard modifiers (Ctrl, Shift, Alt).</param>
        /// <param name="mouseButton">Mouse button released (0=left, 1=right, 2=middle).</param>
        public void HandleMouseUp(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None, int mouseButton = 0)
        {
            _interactionHelper.HandleMouseUp(point, modifiers, mouseButton);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Handles mouse move events for dragging components and drawing connection lines.
        /// </summary>
        /// <param name="point">The point where the mouse move occurred.</param>
        /// <param name="modifiers">Keyboard modifiers (Ctrl, Shift, Alt).</param>
        public void HandleMouseMove(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None)
        {
            _interactionHelper.HandleMouseMove(point, modifiers);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Handles mouse wheel events for zooming.
        /// </summary>
        /// <param name="point">The point where the mouse wheel event occurred.</param>
        /// <param name="delta">The wheel delta value.</param>
        public void HandleMouseWheel(SKPoint point, float delta)
        {
            _interactionHelper.HandleMouseWheel(point, delta);
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        public void Undo()
        {
            _historyManager.Undo();
        }

        /// <summary>
        /// Redoes the last undone action.
        /// </summary>
        public void Redo()
        {
            _historyManager.Redo();
        }

        /// <summary>
        /// Gets a value indicating whether undo is available.
        /// </summary>
        public bool CanUndo => _historyManager.CanUndo;

        /// <summary>
        /// Gets a value indicating whether redo is available.
        /// </summary>
        public bool CanRedo => _historyManager.CanRedo;

        /// <summary>
        /// Handles keyboard input for diagram-wide shortcuts.
        /// Call this from the host control's key event handler.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <param name="modifiers">Bitmask: Ctrl=1, Shift=2, Alt=4.</param>
        /// <returns>True if the key was handled.</returns>
        public bool HandleKeyDown(int key, int modifiers = 0)
        {
            bool ctrl = (modifiers & 1) != 0;
            bool shift = (modifiers & 2) != 0;

            // Ctrl key combinations
            if (ctrl)
            {
                switch (key)
                {
                    case 90: // Z - Undo
                        if (CanUndo) { Undo(); DrawSurface?.Invoke(this, null); }
                        return true;
                    case 89: // Y - Redo
                        if (CanRedo) { Redo(); DrawSurface?.Invoke(this, null); }
                        return true;
                    case 67: // C - Copy
                        CopySelectedComponents();
                        return true;
                    case 86: // V - Paste
                        PasteComponents(SKPoint.Empty); // Paste at origin (offset applied)
                        return true;
                    case 88: // X - Cut
                        CopySelectedComponents();
                        DeleteSelectedComponents();
                        return true;
                    case 65: // A - Select All
                        _selectionManager.SelectAll();
                        DrawSurface?.Invoke(this, null);
                        return true;
                    case 83: // S - Save (triggers event for host to handle)
                        SaveRequested?.Invoke(this, EventArgs.Empty);
                        return true;
                    case 71: // G - Toggle Grid
                        ShowGrid = !ShowGrid;
                        return true;
                    case 84: // T - Toggle Theme
                        ThemeManager.ApplyTheme(ThemeManager.Current.Name == "Dark" ? "Light" : "Dark");
                        DrawSurface?.Invoke(this, null);
                        return true;
                }
            }

            // Non-ctrl keys
            switch (key)
            {
                case 9: // Tab / Shift+Tab — cycle selection (keyboard accessibility)
                    SelectNextComponent(forward: !shift);
                    return true;
                case 46: // Delete / Backspace
                case 8:
                    if (_selectionManager.SelectionCount > 0)
                    {
                        DeleteSelectedComponents();
                        return true;
                    }
                    break;
                case 27: // Escape
                    _selectionManager.ClearSelection();
                    DrawSurface?.Invoke(this, null);
                    return true;
                case 37: // Left Arrow
                    MoveSelectedComponents(new SKPoint(shift ? -10 : -1, 0));
                    return true;
                case 39: // Right Arrow
                    MoveSelectedComponents(new SKPoint(shift ? 10 : 1, 0));
                    return true;
                case 38: // Up Arrow
                    MoveSelectedComponents(new SKPoint(0, shift ? -10 : -1));
                    return true;
                case 40: // Down Arrow
                    MoveSelectedComponents(new SKPoint(0, shift ? 10 : 1));
                    return true;
                case 187: // Plus/Equals
                case 107: // Numpad Plus
                    Zoom = Math.Min(5f, Zoom * 1.1f);
                    return true;
                case 189: // Minus
                case 109: // Numpad Minus
                    Zoom = Math.Max(0.1f, Zoom / 1.1f);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Occurs when Ctrl+S is pressed, allowing the host to save.
        /// </summary>
        public event EventHandler SaveRequested;
    }
}
