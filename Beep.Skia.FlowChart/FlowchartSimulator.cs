using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// A candidate transition from the current simulation node.
    /// </summary>
    public class SimulationEdge
    {
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public IConnectionLine? Line { get; set; }
        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        public SkiaComponent? Target { get; set; }
        /// <summary>
        /// Gets or sets the port index.
        /// </summary>
        public int PortIndex { get; set; }
    }

    /// <summary>
    /// Event args raised after each simulation step.
    /// </summary>
    public class SimulationStepEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the node.
        /// </summary>
        public SkiaComponent? Node { get; set; }
        /// <summary>
        /// Gets or sets the outgoing.
        /// </summary>
        public IReadOnlyList<SimulationEdge> Outgoing { get; set; } = new List<SimulationEdge>();
        /// <summary>
        /// Gets or sets the is finished.
        /// </summary>
        public bool IsFinished { get; set; }
    }

    /// <summary>
    /// Step-through execution of a flowchart. The simulator walks connection lines in port order;
    /// when a node has multiple outgoing edges (decisions, loops, forks) the optional
    /// <see cref="BranchSelector"/> chooses the branch (defaults to the first edge).
    /// </summary>
    public class FlowchartSimulator
    {
        private IReadOnlyList<IConnectionLine> _lines = new List<IConnectionLine>();
        private readonly HashSet<SkiaComponent> _visited = new HashSet<SkiaComponent>();

        /// <summary>
        /// Gets the node the simulation is currently on.
        /// </summary>
        public SkiaComponent? CurrentNode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the simulation reached a terminal node.
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// Gets the number of steps executed since the last reset.
        /// </summary>
        public int StepCount { get; private set; }

        /// <summary>
        /// Gets the execution trace (node labels in visit order).
        /// </summary>
        public List<string> Trace { get; } = new List<string>();

        /// <summary>
        /// Optional branch selector for nodes with multiple outgoing edges.
        /// Return null to fall back to the first edge.
        /// </summary>
        public Func<SkiaComponent, IReadOnlyList<SimulationEdge>, SimulationEdge?>? BranchSelector { get; set; }

        /// <summary>
        /// Raised after each step.
        /// </summary>
        public event EventHandler<SimulationStepEventArgs>? Stepped;

        /// <summary>
        /// Resets the simulation to the entry node of the given diagram.
        /// </summary>
        public void Reset(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            _lines = lines ?? new List<IConnectionLine>();
            _visited.Clear();
            Trace.Clear();
            StepCount = 0;
            IsFinished = false;
            CurrentNode = FindEntry(components, _lines);
            if (CurrentNode == null) IsFinished = true;
        }

        /// <summary>
        /// Advances the simulation by one node. Returns false when already finished.
        /// </summary>
        public bool Step()
        {
            if (IsFinished || CurrentNode == null) return false;

            var node = CurrentNode;
            StepCount++;
            _visited.Add(node);
            Trace.Add(GetLabel(node));

            var outgoing = GetOutgoing(node);

            // Terminal node: no outgoing edges (or an end terminator).
            if (outgoing.Count == 0 || IsEndNode(node))
            {
                IsFinished = true;
                Stepped?.Invoke(this, new SimulationStepEventArgs { Node = node, Outgoing = outgoing, IsFinished = true });
                return true;
            }

            var edge = SelectEdge(node, outgoing);
            CurrentNode = edge?.Target;
            if (CurrentNode == null) IsFinished = true;

            Stepped?.Invoke(this, new SimulationStepEventArgs
            {
                Node = node,
                Outgoing = outgoing,
                IsFinished = IsFinished
            });
            return true;
        }

        /// <summary>
        /// Runs until finished or the step cap is reached.
        /// </summary>
        /// <returns>True when the simulation finished naturally; false when capped.</returns>
        public bool Run(int maxSteps = 1000)
        {
            int guard = 0;
            while (!IsFinished && guard++ < maxSteps)
            {
                Step();
            }
            return IsFinished;
        }

        private SimulationEdge SelectEdge(SkiaComponent node, IReadOnlyList<SimulationEdge> outgoing)
        {
            if (outgoing.Count == 1) return outgoing[0];
            var selected = BranchSelector?.Invoke(node, outgoing);
            return selected ?? outgoing[0];
        }

        private IReadOnlyList<SimulationEdge> GetOutgoing(SkiaComponent node)
        {
            var result = new List<SimulationEdge>();
            if (node == null) return result;

            foreach (var line in _lines)
            {
                if (line?.Start?.Component == node && line.End?.Component is SkiaComponent target)
                {
                    result.Add(new SimulationEdge
                    {
                        Line = line,
                        Target = target,
                        PortIndex = GetPortIndex(node, line.Start)
                    });
                }
            }

            return result
                .OrderBy(e => e.PortIndex)
                .ThenBy(e => e.Target!.Y)
                .ThenBy(e => e.Target!.X)
                .ToList();
        }

        private static int GetPortIndex(SkiaComponent node, IConnectionPoint port)
        {
            var outPoints = node.OutConnectionPoints;
            if (outPoints != null)
            {
                var index = outPoints.IndexOf(port);
                if (index >= 0) return index;
            }
            return port?.Index ?? int.MaxValue;
        }

        private static SkiaComponent? FindEntry(IReadOnlyList<SkiaComponent>? components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null) return null;

            var nodes = components
                .Where(c => c is FlowchartControl && !(c is AnnotationNode) && !c.IsStatic)
                .ToList();
            if (nodes.Count == 0) return null;

            var starts = nodes.OfType<StartEndNode>().ToList();
            if (starts.Count > 0)
            {
                return starts.FirstOrDefault(s => !HasIncoming(s, lines)) ?? starts[0];
            }

            return nodes.FirstOrDefault(n => !HasIncoming(n, lines)) ?? nodes[0];
        }

        private static bool HasIncoming(SkiaComponent node, IReadOnlyList<IConnectionLine> lines)
            => lines != null && lines.Any(l => l?.End?.Component == node);

        private static bool IsEndNode(SkiaComponent node)
        {
            if (!(node is StartEndNode)) return false;
            var label = GetLabel(node);
            return !string.IsNullOrWhiteSpace(label) &&
                   label.IndexOf("end", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetLabel(SkiaComponent node)
        {
            if (node == null) return "(none)";

            foreach (var name in new[] { "Label", "Text", "Condition", "Title", "SubProcessId" })
            {
                var prop = node.GetType().GetProperty(name);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(node) as string;
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }
            }

            return node.Name ?? node.GetType().Name;
        }
    }
}
