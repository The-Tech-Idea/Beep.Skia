using System;
using System.Collections.Generic;
using Beep.Skia.Serialization;

namespace Beep.Skia.Assist
{
    /// <summary>
    /// A request for an AI/assisted diagram generation.
    /// </summary>
    public class DiagramRequest
    {
        /// <summary>Natural-language or DSL description of the diagram.</summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>Optional kind hint: "flowchart" (default) or "mindmap".</summary>
        public string DiagramKind { get; set; }

        /// <summary>Maximum number of nodes to generate.</summary>
        public int MaxNodes { get; set; } = 60;
    }

    /// <summary>
    /// The result of an assisted generation attempt.
    /// </summary>
    public class DiagramSuggestion
    {
        public bool Success { get; set; }

        /// <summary>Generated diagram, loadable via DrawingManager.LoadFromDto.</summary>
        public DiagramDto Diagram { get; set; }

        /// <summary>Human-readable summary of what was generated.</summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>Non-fatal issues encountered while generating.</summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>Name of the assistant that produced this suggestion.</summary>
        public string Provider { get; set; } = string.Empty;

        public static DiagramSuggestion Failed(string provider, string reason)
        {
            var suggestion = new DiagramSuggestion { Success = false, Provider = provider };
            if (!string.IsNullOrWhiteSpace(reason)) suggestion.Warnings.Add(reason);
            return suggestion;
        }
    }

    /// <summary>
    /// Contract for diagram assistants (rule-based, local model, or cloud LLM).
    /// Implementations must be safe to call synchronously and must not throw for bad input.
    /// </summary>
    public interface IDiagramAssistant
    {
        /// <summary>Display name of the assistant/provider.</summary>
        string Name { get; }

        /// <summary>Generates a diagram suggestion for the request.</summary>
        DiagramSuggestion Generate(DiagramRequest request);
    }

    /// <summary>
    /// Tries registered assistants in order and returns the first successful suggestion,
    /// falling back through providers. Always contains the rule-based assistant by default.
    /// </summary>
    public class DiagramAssistantRegistry
    {
        private readonly List<IDiagramAssistant> _assistants = new List<IDiagramAssistant>();

        public DiagramAssistantRegistry(bool includeDefaultAssistant = true)
        {
            if (includeDefaultAssistant) _assistants.Add(new RuleBasedDiagramAssistant());
        }

        public IReadOnlyList<IDiagramAssistant> Assistants => _assistants.AsReadOnly();

        /// <summary>Adds an assistant at the given priority (0 = first to be tried).</summary>
        public void Register(IDiagramAssistant assistant, int priority = 0)
        {
            if (assistant == null) throw new ArgumentNullException(nameof(assistant));
            var index = Math.Max(0, Math.Min(priority, _assistants.Count));
            _assistants.Insert(index, assistant);
        }

        /// <summary>Removes an assistant by name.</summary>
        public bool Unregister(string name)
            => _assistants.RemoveAll(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;

        /// <summary>
        /// Generates a suggestion using the first assistant that succeeds. Warnings from
        /// failed attempts are carried onto the final result.
        /// </summary>
        public DiagramSuggestion Generate(DiagramRequest request)
        {
            request ??= new DiagramRequest();
            var carried = new List<string>();

            foreach (var assistant in _assistants)
            {
                DiagramSuggestion result;
                try
                {
                    result = assistant.Generate(request) ?? DiagramSuggestion.Failed(assistant.Name, "Assistant returned no result.");
                }
                catch (Exception ex)
                {
                    result = DiagramSuggestion.Failed(assistant.Name, ex.Message);
                }

                if (result.Success) return result;

                carried.Add($"{assistant.Name}: {string.Join("; ", result.Warnings)}");
            }

            var failed = DiagramSuggestion.Failed("none", "No assistant could generate a diagram for this request.");
            failed.Warnings.InsertRange(0, carried);
            return failed;
        }
    }
}
