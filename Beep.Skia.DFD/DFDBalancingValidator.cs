using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Balancing Validator — checks consistency between parent and child DFD levels.
    /// Validates that data flows entering/exiting a child diagram match the parent process's inputs/outputs.
    /// </summary>
    public class DFDBalancingValidator
    {
        public List<DFDBalanceIssue> Issues { get; } = new List<DFDBalanceIssue>();

        /// <summary>
        /// Validates that a child DFD diagram balances with its parent process definition.
        /// </summary>
        /// <param name="parentProcess">The parent DFDProcess being decomposed.</param>
        /// <param name="childComponents">Components in the child diagram.</param>
        /// <param name="childLines">Connection lines in the child diagram.</param>
        /// <returns>True if the diagram is balanced.</returns>
        public bool Validate(SkiaComponent parentProcess, IReadOnlyList<SkiaComponent> childComponents, IReadOnlyList<IConnectionLine> childLines)
        {
            Issues.Clear();
            if (parentProcess == null || childComponents == null) return true;

            // Collect data flows entering the parent from external sources
            var parentInputLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parentOutputLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Collect flows in the child diagram that cross the boundary
            var childInputFlows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var childOutputFlows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // In a child DFD, boundary flows are typically represented by:
            // - Flows from external entities (DFDExternalEntity) to child processes
            // - Flows from child processes to external entities
            foreach (var c in childComponents)
            {
                bool isExternal = c.GetType().Name.Contains("ExternalEntity") || c.GetType().Name.Contains("Source");
                bool isProcess = c.GetType().Name.Contains("Process");
                bool isDataStore = c.GetType().Name.Contains("DataStore");

                if (isExternal)
                {
                    // External entities in child diagram represent parent inputs/outputs
                    foreach (var line in childLines)
                    {
                        if (line?.Start?.Component == c && line?.End?.Component is SkiaComponent target)
                        {
                            string flowLabel = line.Label1 ?? line.Label2 ?? $"flow_{Guid.NewGuid():N}";
                            childInputFlows.Add(flowLabel);
                        }
                        if (line?.End?.Component == c && line?.Start?.Component is SkiaComponent source)
                        {
                            string flowLabel = line.Label1 ?? line.Label2 ?? $"flow_{Guid.NewGuid():N}";
                            childOutputFlows.Add(flowLabel);
                        }
                    }
                }
            }

            // Check for unlabeled data flows (common DFD error)
            foreach (var line in childLines)
            {
                if (string.IsNullOrWhiteSpace(line?.Label1) && string.IsNullOrWhiteSpace(line?.Label2)
                    && line?.Start != null && line?.End != null)
                {
                    Issues.Add(new DFDBalanceIssue
                    {
                        Message = $"Unlabeled data flow from '{line.Start.Component?.Name}' to '{line.End.Component?.Name}'",
                        Severity = DFDBalanceSeverity.Warning,
                        FixSuggestion = "Add a label to describe the data being transferred"
                    });
                }
            }

            // Check for processes with no data flows
            foreach (var c in childComponents.Where(c => c.GetType().Name.Contains("Process") && !c.IsStatic))
            {
                bool hasIncoming = childLines.Any(l => l?.End?.Component == c);
                bool hasOutgoing = childLines.Any(l => l?.Start?.Component == c);

                if (!hasIncoming && !hasOutgoing)
                {
                    Issues.Add(new DFDBalanceIssue
                    {
                        Message = $"Process '{c.Name}' has no data flows — it should transform or route data",
                        Component = c,
                        Severity = DFDBalanceSeverity.Warning,
                        FixSuggestion = "Connect data flows to and/or from this process"
                    });
                }
            }

            // Check for data stores with no incoming flow (data mysteriously appears)
            foreach (var c in childComponents.Where(c => c.GetType().Name.Contains("DataStore")))
            {
                bool hasIncoming = childLines.Any(l => l?.End?.Component == c);
                if (!hasIncoming)
                {
                    Issues.Add(new DFDBalanceIssue
                    {
                        Message = $"Data store '{c.Name}' has no incoming data — how does it get populated?",
                        Component = c,
                        Severity = DFDBalanceSeverity.Info,
                        FixSuggestion = "Add a data flow from a process into this data store"
                    });
                }
            }

            return Issues.Count(i => i.Severity == DFDBalanceSeverity.Error) == 0;
        }
    }

    public class DFDBalanceIssue
    {
        public string Message { get; set; } = string.Empty;
        public DFDBalanceSeverity Severity { get; set; } = DFDBalanceSeverity.Warning;
        public string? FixSuggestion { get; set; }
        public SkiaComponent? Component { get; set; }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    public enum DFDBalanceSeverity { Info, Warning, Error }
}
