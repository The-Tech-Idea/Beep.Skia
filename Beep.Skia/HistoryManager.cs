using System;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Helper class for managing undo/redo operations in the drawing manager.
    /// Callers apply a change first and then record the action with <see cref="ExecuteAction"/>;
    /// the action's Execute/Undo methods are used when replaying history.
    /// </summary>
    public class HistoryManager
    {
        private readonly DrawingManager _drawingManager;
        private readonly List<DrawingAction> _undoStack = new List<DrawingAction>();
        private readonly List<DrawingAction> _redoStack = new List<DrawingAction>();
        private bool _isApplyingHistory;
        private Batch _batch;
        /// <summary>Collects already-applied actions into one entry. Disposing without Commit rolls back.</summary>
        public Batch BeginBatch()
        {
            if (_batch != null || _isApplyingHistory) throw new InvalidOperationException("A history operation is already active.");
            return _batch = new Batch(this);
        }
        public sealed class Batch : IDisposable
        {
            private readonly HistoryManager _owner;
            internal readonly List<DrawingAction> Actions = new();
            private bool _finished;
            internal Batch(HistoryManager owner) => _owner = owner;
            public void Commit()
            {
                if (_finished) throw new InvalidOperationException("Batch already finished.");
                _finished = true; _owner._batch = null;
                if (Actions.Count > 0) _owner.ExecuteAction(new GroupAction(Actions.ToArray()));
            }
            public void Dispose()
            {
                if (_finished) return;
                _finished = true; _owner._batch = null; _owner._isApplyingHistory = true;
                try { for (int i = Actions.Count - 1; i >= 0; i--) Actions[i].Undo(); }
                finally { _owner._isApplyingHistory = false; }
            }
        }
        private sealed class GroupAction : DrawingAction
        {
            private readonly DrawingAction[] _actions;
            public GroupAction(DrawingAction[] actions) => _actions = actions;
            public override void Execute() { foreach (var action in _actions) action.Execute(); }
            public override void Undo() { for (int i = _actions.Length - 1; i >= 0; i--) _actions[i].Undo(); }
        }

        /// <summary>
        /// Gets or sets the maximum number of undo steps retained. 0 means unlimited.
        /// </summary>
        public int MaxHistoryDepth { get; set; } = 100;

        /// <summary>
        /// Gets a value indicating whether an undo/redo replay is currently in progress.
        /// While replaying, nested history recording is suppressed to protect the stacks.
        /// </summary>
        public bool IsApplyingHistory => _isApplyingHistory;

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
        }

        /// <summary>
        /// Records an already-applied action on the undo stack.
        /// </summary>
        /// <param name="action">The action to record.</param>
        public void ExecuteAction(DrawingAction action)
        {
            if (action == null) return;
            if (_isApplyingHistory) return; // mutations made while undoing/redoing are not new history
            if (_batch != null) { _batch.Actions.Add(action); return; }

            _undoStack.Add(action);
            TrimHistory();
            _redoStack.Clear(); // Clear redo stack when new action is recorded
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        public void Undo()
        {
            if (_batch != null) throw new InvalidOperationException("Finish the active history batch first.");
            if (_undoStack.Count == 0) return;

            var action = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            _isApplyingHistory = true;
            try
            {
                action.Undo();
            }
            finally
            {
                _isApplyingHistory = false;
            }

            _redoStack.Add(action);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            // Note: DrawSurface is invoked by the DrawingManager's Undo method
        }

        /// <summary>
        /// Redoes the last undone action.
        /// </summary>
        public void Redo()
        {
            if (_batch != null) throw new InvalidOperationException("Finish the active history batch first.");
            if (_redoStack.Count == 0) return;

            var action = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);

            _isApplyingHistory = true;
            try
            {
                action.Execute();
            }
            finally
            {
                _isApplyingHistory = false;
            }

            _undoStack.Add(action);
            TrimHistory();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            // Note: DrawSurface is invoked by the DrawingManager's Redo method
        }

        private void TrimHistory()
        {
            if (MaxHistoryDepth <= 0) return;
            while (_undoStack.Count > MaxHistoryDepth)
            {
                _undoStack.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clears the undo/redo history.
        /// </summary>
        public void Clear()
        {
            if (_batch != null) throw new InvalidOperationException("Finish the active history batch first.");
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
