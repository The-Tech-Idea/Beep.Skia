using SkiaSharp;
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
        public void HandleMouseDown(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None)
        {
            _interactionHelper.HandleMouseDown(point, modifiers);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Handles mouse up events to complete dragging or connection operations.
        /// </summary>
        /// <param name="point">The point where the mouse up occurred.</param>
        /// <param name="modifiers">Keyboard modifiers (Ctrl, Shift, Alt).</param>
        public void HandleMouseUp(SKPoint point, SKKeyModifiers modifiers = SKKeyModifiers.None)
        {
            _interactionHelper.HandleMouseUp(point, modifiers);
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
    }
}
