using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Serialization;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// A level in the DFD decomposition hierarchy.
    /// </summary>
    public class DFDLevelFrame
    {
        /// <summary>
        /// Gets or sets the process name.
        /// </summary>
        public string ProcessName { get; set; } = "Level 0";
        /// <summary>
        /// Gets or sets the diagram.
        /// </summary>
        public DiagramDto? Diagram { get; set; }
    }

    /// <summary>
    /// Manages drill-down navigation across DFD decomposition levels.
    /// The caller captures the current diagram as a <see cref="DiagramDto"/> before drilling in,
    /// and restores frames when navigating up.
    /// </summary>
    public class DFDLevelNavigator
    {
        private readonly Stack<DFDLevelFrame> _stack = new Stack<DFDLevelFrame>();

        /// <summary>
        /// Gets the currently displayed level.
        /// </summary>
        public DFDLevelFrame? Current { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there is a parent level to return to.
        /// </summary>
        public bool CanGoUp => _stack.Count > 0;

        /// <summary>
        /// Gets the current decomposition depth (0 = top level).
        /// </summary>
        public int Depth => _stack.Count;

        /// <summary>
        /// Gets a breadcrumb like "Level 0 &gt; Process 1 &gt; Process 1.1".
        /// </summary>
        public string Breadcrumb
        {
            get
            {
                var names = _stack.Reverse().Select(f => f.ProcessName).ToList();
                names.Add(Current?.ProcessName ?? "Level 0");
                return string.Join(" > ", names);
            }
        }

        /// <summary>
        /// Resets navigation to the top-level diagram.
        /// </summary>
        public void Reset(DiagramDto? rootDiagram = null)
        {
            _stack.Clear();
            Current = new DFDLevelFrame { ProcessName = "Level 0", Diagram = rootDiagram };
        }

        /// <summary>
        /// Drills into a process's child decomposition diagram.
        /// </summary>
        /// <param name="processName">Display name of the process being decomposed.</param>
        /// <param name="currentDiagram">Live snapshot of the currently displayed diagram (pushed as the parent frame).</param>
        /// <param name="childDiagram">The child diagram to display.</param>
        /// <returns>True when the drill-down started.</returns>
        public bool DrillDown(string processName, DiagramDto currentDiagram, DiagramDto childDiagram)
        {
            if (childDiagram == null) return false;

            var frame = Current ?? new DFDLevelFrame { ProcessName = "Level 0" };
            if (currentDiagram != null) frame.Diagram = currentDiagram;
            _stack.Push(frame);

            Current = new DFDLevelFrame
            {
                ProcessName = string.IsNullOrWhiteSpace(processName) ? $"Level {_stack.Count}" : processName,
                Diagram = childDiagram
            };
            return true;
        }

        /// <summary>
        /// Returns to the parent level.
        /// </summary>
        /// <returns>The frame now displayed, or null when already at the top level.</returns>
        public DFDLevelFrame? GoUp()
        {
            if (_stack.Count == 0) return null;
            Current = _stack.Pop();
            return Current;
        }
    }
}
