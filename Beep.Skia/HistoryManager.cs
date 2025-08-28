using System;
using System.Collections.Generic;

namespace Beep.Skia
{
    /// <summary>
    /// Helper class for managing undo/redo operations in the drawing manager.
    /// </summary>
    public class HistoryManager
    {
        private readonly DrawingManager _drawingManager;
        private readonly Stack<DrawingAction> _undoStack;
        private readonly Stack<DrawingAction> _redoStack;

        /// <summary>
        /// Gets a value indicating whether undo is available.
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Gets a value indicating whether redo is available.
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Occurs when the undo/redo state changes.
        /// </summary>
        public event EventHandler HistoryChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryManager"/> class.
        /// </summary>
        /// <param name="drawingManager">The drawing manager that owns this history manager.</param>
        public HistoryManager(DrawingManager drawingManager)
        {
            _drawingManager = drawingManager;
            _undoStack = new Stack<DrawingAction>();
            _redoStack = new Stack<DrawingAction>();
        }

        /// <summary>
        /// Executes an action and adds it to the undo stack.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void ExecuteAction(DrawingAction action)
        {
            action.Execute();
            _undoStack.Push(action);
            _redoStack.Clear(); // Clear redo stack when new action is executed
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Pop();
                action.Undo();
                _redoStack.Push(action);
                HistoryChanged?.Invoke(this, EventArgs.Empty);
                // Note: DrawSurface is invoked by the DrawingManager's Undo method
            }
        }

        /// <summary>
        /// Redoes the last undone action.
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                action.Execute();
                _undoStack.Push(action);
                HistoryChanged?.Invoke(this, EventArgs.Empty);
                // Note: DrawSurface is invoked by the DrawingManager's Redo method
            }
        }

        /// <summary>
        /// Clears the undo/redo history.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the number of actions that can be undone.
        /// </summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>
        /// Gets the number of actions that can be redone.
        /// </summary>
        public int RedoCount => _redoStack.Count;
    }
}
