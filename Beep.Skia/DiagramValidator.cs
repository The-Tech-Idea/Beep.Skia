using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia
{
    /// <summary>
    /// Validation result for a diagram issue.
    /// </summary>
    public class DiagramIssue
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public DiagramIssueSeverity Severity { get; set; } = DiagramIssueSeverity.Warning;
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public SkiaComponent Component { get; set; }
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public IConnectionLine Line { get; set; }
        /// <summary>
        /// Gets or sets the rule name.
        /// </summary>
        public string RuleName { get; set; }
        /// <summary>
        /// Gets or sets the fix suggestion.
        /// </summary>
        public string FixSuggestion { get; set; }

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString()
        {
            var target = Component?.Name ?? Line?.ToString() ?? "diagram";
            return $"[{Severity}] {RuleName}: {Message} ({target})";
        }
    }

    public enum DiagramIssueSeverity { Info, Warning, Error }

    /// <summary>
    /// Plugable validation rule for a diagram.
    /// </summary>
    public interface IDiagramRule
    {
        string Name { get; }
        string Description { get; }
        IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines);
    }

    /// <summary>
    /// Validates a diagram against a set of pluggable rules.
    /// Call DrawingManager.ValidateDiagram() to run all registered rules.
    /// </summary>
    public class DiagramValidator
    {
        private readonly List<IDiagramRule> _rules = new List<IDiagramRule>();

        /// <summary>
        /// Gets or sets the rules.
        /// </summary>
        public IReadOnlyList<IDiagramRule> Rules => _rules;

        /// <summary>
        /// Gets or sets the add rule.
        /// </summary>
        public void AddRule(IDiagramRule rule)
        {
            if (rule != null && !_rules.Any(r => r.Name == rule.Name))
                _rules.Add(rule);
        }

        /// <summary>
        /// Gets or sets the add default rules.
        /// </summary>
        public void AddDefaultRules()
        {
            AddRule(new OrphanNodeRule());
            AddRule(new CycleDetectionRule());
            AddRule(new DeadEndNodeRule());
            AddRule(new OverlappingComponentsRule());
            AddRule(new EmptyDiagramRule());
            AddRule(new UnconnectedPortRule());
        }

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public List<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var issues = new List<DiagramIssue>();
            foreach (var rule in _rules)
            {
                try { issues.AddRange(rule.Validate(components, lines)); }
                catch { }
            }
            return issues;
        }
    }

    // =========================================================================
    // Built-in Rules
    // =========================================================================

    /// <summary>
    /// Detects nodes that have no connections (orphan nodes).
    /// </summary>
    public class OrphanNodeRule : IDiagramRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "OrphanNodes";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description => "Detects components with zero incoming and zero outgoing connections.";

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var connected = new HashSet<SkiaComponent>();
            foreach (var line in lines)
            {
                if (line?.Start?.Component is SkiaComponent s) connected.Add(s);
                if (line?.End?.Component is SkiaComponent e) connected.Add(e);
            }

            foreach (var c in components.Where(c => !c.IsStatic))
            {
                if (!connected.Contains(c))
                {
                    yield return new DiagramIssue
                    {
                        Message = $"'{c.Name}' has no connections (orphan node)",
                        Severity = DiagramIssueSeverity.Warning,
                        Component = c,
                        RuleName = Name,
                        FixSuggestion = "Connect this node to another or remove it if unnecessary"
                    };
                }
            }
        }
    }

    /// <summary>
    /// Detects cycles in the directed graph (excluding undirected links).
    /// </summary>
    public class CycleDetectionRule : IDiagramRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "CycleDetection";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description => "Detects cycles in the diagram that may cause infinite loops.";

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var index = new Dictionary<SkiaComponent, int>();
            for (int i = 0; i < components.Count; i++) index[components[i]] = i;
            int n = components.Count;
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();

            foreach (var line in lines)
            {
                if (line?.Start?.Component is SkiaComponent s && line?.End?.Component is SkiaComponent e
                    && index.TryGetValue(s, out int si) && index.TryGetValue(e, out int ei) && si != ei)
                {
                    adj[si].Add(ei);
                }
            }

            var state = new int[n]; // 0=unvisited, 1=visiting, 2=visited
            var path = new Stack<int>();

            for (int i = 0; i < n; i++)
            {
                if (state[i] != 0) continue;
                if (HasCycle(i, adj, state, path, components))
                {
                    yield return new DiagramIssue
                    {
                        Message = "Cycle detected in diagram — may indicate a logic loop",
                        Severity = DiagramIssueSeverity.Warning,
                        RuleName = Name,
                        FixSuggestion = "Review connections to break the cycle or add an exit condition"
                    };
                    yield break; // One cycle report is enough
                }
            }
        }

        private bool HasCycle(int v, List<int>[] adj, int[] state, Stack<int> path, IReadOnlyList<SkiaComponent> comps)
        {
            state[v] = 1;
            foreach (int w in adj[v])
            {
                if (state[w] == 1) return true;
                if (state[w] == 0 && HasCycle(w, adj, state, path, comps)) return true;
            }
            state[v] = 2;
            return false;
        }
    }

    /// <summary>
    /// Detects nodes that have inputs but no outputs (dead ends).
    /// </summary>
    public class DeadEndNodeRule : IDiagramRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "DeadEndNodes";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description => "Detects components with input connections but no output connections (possible dead ends).";

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            foreach (var c in components.Where(c => !c.IsStatic))
            {
                bool hasInput = lines.Any(l => l?.End?.Component == c);
                bool hasOutput = lines.Any(l => l?.Start?.Component == c);

                if (hasInput && !hasOutput)
                {
                    yield return new DiagramIssue
                    {
                        Message = $"'{c.Name}' receives input but has no output (dead end)",
                        Severity = DiagramIssueSeverity.Info,
                        Component = c,
                        RuleName = Name,
                        FixSuggestion = "Add an outgoing connection or designate as terminal node"
                    };
                }
            }
        }
    }

    /// <summary>
    /// Detects components whose bounding rectangles overlap significantly.
    /// </summary>
    public class OverlappingComponentsRule : IDiagramRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "OverlappingComponents";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description => "Detects components that overlap by more than 50% of the smaller area.";

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var active = components.Where(c => !c.IsStatic).ToList();
            for (int i = 0; i < active.Count; i++)
            {
                for (int j = i + 1; j < active.Count; j++)
                {
                    var a = active[i].Bounds;
                    var b = active[j].Bounds;
                    var overlap = SKRect.Intersect(a, b);
                    if (overlap.IsEmpty) continue;

                    float overlapArea = overlap.Width * overlap.Height;
                    float minArea = Math.Min(a.Width * a.Height, b.Width * b.Height);
                    if (minArea > 0 && overlapArea / minArea > 0.5f)
                    {
                        yield return new DiagramIssue
                        {
                            Message = $"'{active[i].Name}' overlaps '{active[j].Name}' by >50%",
                            Severity = DiagramIssueSeverity.Warning,
                            Component = active[i],
                            RuleName = Name,
                            FixSuggestion = "Move components apart or use ArrangeDiagram() for auto-layout"
                        };
                    }
                }
            }
        }
    }

    /// <summary>
    /// Warns when the diagram is empty.
    /// </summary>
    public class EmptyDiagramRule : IDiagramRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "EmptyDiagram";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description => "Checks whether the diagram contains any components.";

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components.Count(c => !c.IsStatic) == 0)
            {
                yield return new DiagramIssue
                {
                    Message = "Diagram is empty — no components present",
                    Severity = DiagramIssueSeverity.Info,
                    RuleName = Name,
                    FixSuggestion = "Add components via the palette or drag-and-drop"
                };
            }
        }
    }

    /// <summary>
    /// Detects connection lines where endpoints are not actually connected to ports.
    /// </summary>
    public class UnconnectedPortRule : IDiagramRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "UnconnectedPorts";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description => "Detects lines that reference null or removed connection points.";

        /// <summary>
        /// Gets or sets the validate.
        /// </summary>
        public IEnumerable<DiagramIssue> Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            foreach (var line in lines)
            {
                if (line == null) continue;
                if (line.Start == null)
                {
                    yield return new DiagramIssue
                    {
                        Message = "A connection line has no start point",
                        Severity = DiagramIssueSeverity.Error,
                        Line = line,
                        RuleName = Name,
                        FixSuggestion = "Reconnect the line or delete it"
                    };
                }
                else if (line.End == null)
                {
                    yield return new DiagramIssue
                    {
                        Message = $"Connection from '{line.Start.Component?.Name}' has no end point",
                        Severity = DiagramIssueSeverity.Error,
                        Line = line,
                        RuleName = Name,
                        FixSuggestion = "Reconnect the line or delete it"
                    };
                }
            }
        }
    }
}
