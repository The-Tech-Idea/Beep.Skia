using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Serialization;

namespace Beep.Skia.Assist
{
    /// <summary>
    /// Offline, deterministic diagram assistant. Understands the flowchart DSL
    /// (see <see cref="FlowchartDslParser"/>) and indentation-based mind-map outlines
    /// (see <see cref="MindMapDslParser"/>), lays the result out, and produces a
    /// <see cref="DiagramDto"/> ready for DrawingManager.LoadFromDto.
    /// </summary>
    public class RuleBasedDiagramAssistant : IDiagramAssistant
    {
        public string Name => "rule-based";

        public DiagramSuggestion Generate(DiagramRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return DiagramSuggestion.Failed(Name, "The description is empty.");

            var kind = (request.DiagramKind ?? string.Empty).ToLowerInvariant();
            var wantsMindMap = kind.Contains("mind") ||
                               (kind.Length == 0 && !request.Prompt.Contains("->") && LooksIndented(request.Prompt));

            return wantsMindMap ? GenerateMindMap(request) : GenerateFlowchart(request);
        }

        private DiagramSuggestion GenerateFlowchart(DiagramRequest request)
        {
            var graph = FlowchartDslParser.Parse(request.Prompt, request.MaxNodes);
            var suggestion = new DiagramSuggestion { Provider = Name };

            if (graph.Nodes.Count == 0)
            {
                suggestion.Warnings.Add("No nodes were recognized. Use lines like: Start -> Process \"Do work\" -> End");
                suggestion.Warnings.AddRange(graph.Warnings);
                return suggestion;
            }

            var dto = new DiagramDto();
            foreach (var node in graph.Nodes)
            {
                var component = new ComponentDto
                {
                    Type = Aqn("Flowchart", "Beep.Skia.FlowChart", node.ClassName),
                    X = node.X,
                    Y = node.Y,
                    Width = node.Width,
                    Height = node.Height,
                    Name = node.Name
                };
                component.PropertyBag["Title"] = node.Title;
                dto.Components.Add(component);
            }

            foreach (var edge in graph.Edges)
            {
                var from = graph.Nodes.FindIndex(n => string.Equals(n.Name, edge.From, StringComparison.OrdinalIgnoreCase));
                var to = graph.Nodes.FindIndex(n => string.Equals(n.Name, edge.To, StringComparison.OrdinalIgnoreCase));
                if (from < 0 || to < 0) continue;

                var line = new LineDto
                {
                    StartComponentIndex = from,
                    EndComponentIndex = to,
                    Label1 = edge.Label
                };
                dto.Lines.Add(line);
            }

            suggestion.Success = true;
            suggestion.Diagram = dto;
            suggestion.Explanation =
                $"Flowchart with {dto.Components.Count} node(s) and {dto.Lines.Count} connection(s).";
            suggestion.Warnings.AddRange(graph.Warnings);
            return suggestion;
        }

        private DiagramSuggestion GenerateMindMap(DiagramRequest request)
        {
            var root = MindMapDslParser.Parse(request.Prompt, out var warnings, request.MaxNodes);
            var suggestion = new DiagramSuggestion { Provider = Name };
            suggestion.Warnings.AddRange(warnings);

            if (root == null)
            {
                suggestion.Warnings.Add("No mind-map content was recognized. Indent child lines with spaces.");
                return suggestion;
            }

            var dto = new DiagramDto();
            var index = new Dictionary<MindMapDslNode, int>();
            var ordered = new List<MindMapDslNode>();

            void Visit(MindMapDslNode node)
            {
                index[node] = ordered.Count;
                ordered.Add(node);
                foreach (var child in node.Children) Visit(child);
            }
            Visit(root);

            foreach (var node in ordered)
            {
                var component = new ComponentDto
                {
                    Type = Aqn("MindMap", "Beep.Skia.MindMap", ClassFor(node.TypeKeyword)),
                    X = node.X,
                    Y = node.Y,
                    Width = node.Width,
                    Height = node.Height,
                    Name = node.Title
                };
                component.PropertyBag["Title"] = node.Title;
                dto.Components.Add(component);
            }

            foreach (var node in ordered)
            {
                foreach (var child in node.Children)
                {
                    dto.Lines.Add(new LineDto
                    {
                        StartComponentIndex = index[node],
                        EndComponentIndex = index[child]
                    });
                }
            }

            suggestion.Success = true;
            suggestion.Diagram = dto;
            suggestion.Explanation =
                $"Mind map with {dto.Components.Count} node(s) and {dto.Lines.Count} connection(s).";
            return suggestion;
        }

        private static string ClassFor(string typeKeyword)
        {
            switch (typeKeyword)
            {
                case "central": return "CentralNode";
                case "topic": return "TopicNode";
                case "subtopic": return "SubTopicNode";
                default: return "ProcessNode";
            }
        }

        private static string Aqn(string ns, string assembly, string cls)
            => $"Beep.Skia.{ns}.{cls}, {assembly}";

        private static bool LooksIndented(string prompt)
        {
            foreach (var raw in prompt.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.Trim().Length == 0) continue;
                if (raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t')) return true;
            }
            return false;
        }
    }
}
