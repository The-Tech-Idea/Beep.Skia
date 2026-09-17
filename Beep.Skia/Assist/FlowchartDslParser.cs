using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beep.Skia.Assist
{
    /// <summary>A parsed DSL node with layout geometry.</summary>
    public class DslNode
    {
        public string Name { get; set; } = string.Empty;
        public string TypeKeyword { get; set; } = "process";

        /// <summary>Resolved component class name (e.g. ProcessNode, DecisionNode).</summary>
        public string ClassName { get; set; } = "ProcessNode";

        public string Title { get; set; } = string.Empty;
        public int Rank { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; } = 170f;
        public float Height { get; set; } = 54f;
    }

    /// <summary>A parsed DSL edge.</summary>
    public class DslEdge
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Label { get; set; }
    }

    /// <summary>Parsed graph of nodes and edges with warnings.</summary>
    public class DslGraph
    {
        public List<DslNode> Nodes { get; } = new List<DslNode>();
        public List<DslEdge> Edges { get; } = new List<DslEdge>();
        public List<string> Warnings { get; } = new List<string>();

        public DslNode Find(string name)
            => Nodes.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parses a compact flowchart DSL into a graph and lays it out top-down.
    ///
    /// Grammar (one statement per line):
    ///   node ["(type)"] ["title"] ["->" node ...] [": edge label"]
    /// Examples:
    ///   Start(start) "Start" -> Receive "Receive order" -> Valid(decision) "Valid?"
    ///   Valid -> Process "Process payment" : yes
    ///   Valid -> Notify "Notify customer" : no
    ///   Process -> End(end) "End"
    /// Lines starting with # or // are comments. Node names are case-insensitive;
    /// a node is created on first mention and reused afterwards.
    /// </summary>
    public static class FlowchartDslParser
    {
        private static readonly Dictionary<string, (string Class, float W, float H)> TypeMap =
            new Dictionary<string, (string, float, float)>(StringComparer.OrdinalIgnoreCase)
            {
                ["start"] = ("StartEndNode", 120f, 44f),
                ["end"] = ("StartEndNode", 120f, 44f),
                ["process"] = ("ProcessNode", 170f, 54f),
                ["decision"] = ("DecisionNode", 150f, 80f),
                ["if"] = ("DecisionNode", 150f, 80f),
                ["io"] = ("InputOutputNode", 170f, 60f),
                ["input"] = ("InputOutputNode", 170f, 60f),
                ["output"] = ("InputOutputNode", 170f, 60f),
                ["document"] = ("DocumentNode", 170f, 60f),
                ["data"] = ("DataNode", 170f, 60f),
                ["delay"] = ("DelayNode", 150f, 60f),
                ["wait"] = ("DelayNode", 150f, 60f),
                ["manual"] = ("ManualOperationNode", 170f, 54f),
                ["manualinput"] = ("ManualInputNode", 170f, 54f),
                ["subprocess"] = ("SubProcessNode", 170f, 54f),
                ["predefined"] = ("PredefinedProcessNode", 170f, 54f),
                ["preparation"] = ("PreparationNode", 170f, 54f),
                ["connector"] = ("ConnectorNode", 60f, 60f),
                ["merge"] = ("MergeNode", 90f, 60f),
                ["fork"] = ("ForkNode", 150f, 30f),
                ["join"] = ("JoinNode", 150f, 30f),
                ["display"] = ("DisplayNode", 170f, 60f),
                ["sort"] = ("SortNode", 170f, 60f),
                ["extract"] = ("ExtractNode", 170f, 60f),
                ["collate"] = ("CollateNode", 170f, 60f),
                ["stored"] = ("StoredDataNode", 170f, 60f),
                ["storeddata"] = ("StoredDataNode", 170f, 60f),
                ["internalstorage"] = ("InternalStorageNode", 170f, 60f),
                ["offpage"] = ("OffPageReferenceNode", 120f, 54f),
                ["annotation"] = ("AnnotationNode", 150f, 50f),
                ["comment"] = ("CommentNode", 150f, 50f),
                ["card"] = ("CardNode", 150f, 70f),
                ["dowhile"] = ("DoWhileLoopNode", 170f, 60f),
                ["forloop"] = ("ForLoopNode", 170f, 60f),
                ["looplimit"] = ("LoopLimitNode", 170f, 54f),
                ["or"] = ("OrNode", 120f, 60f),
                ["directaccess"] = ("DirectAccessNode", 170f, 60f)
            };

        /// <summary>Parses DSL text into a graph and applies the top-down layout.</summary>
        public static DslGraph Parse(string text, int maxNodes = 60)
        {
            var graph = new DslGraph();
            if (string.IsNullOrWhiteSpace(text))
            {
                graph.Warnings.Add("The description is empty.");
                return graph;
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var lineNumber = 0;

            foreach (var raw in lines)
            {
                lineNumber++;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#") || line.StartsWith("//")) continue;

                var (body, edgeLabel) = SplitTrailingLabel(line);
                var segments = SplitTopLevel(body, "->");

                DslNode previous = null;
                foreach (var segment in segments)
                {
                    var node = ParseSegment(segment, graph, lineNumber);
                    if (node == null) continue;

                    if (graph.Nodes.Count > Math.Max(1, maxNodes))
                    {
                        graph.Nodes.Remove(node);
                        graph.Warnings.Add($"Node limit of {maxNodes} reached; remaining statements were ignored.");
                        return Finish(graph);
                    }

                    if (previous != null && !ReferenceEquals(previous, node))
                        graph.Edges.Add(new DslEdge { From = previous.Name, To = node.Name, Label = edgeLabel });

                    previous = node;
                }
            }

            return Finish(graph);
        }

        private static DslGraph Finish(DslGraph graph)
        {
            Layout(graph);
            return graph;
        }

        private static DslNode ParseSegment(string segment, DslGraph graph, int lineNumber)
        {
            var text = (segment ?? string.Empty).Trim();
            if (text.Length == 0) return null;

            string name = null;
            string title = null;
            string typeKeyword = null;

            var i = 0;
            if (text[0] == '"' || text[0] == '\'')
            {
                title = ReadQuoted(text, ref i);
                name = Slug(title);
            }
            else
            {
                var start = i;
                while (i < text.Length && text[i] != '(' && text[i] != '"' && text[i] != '\'') i++;
                name = text.Substring(start, i - start).Trim();
            }

            SkipSpaces(text, ref i);
            if (i < text.Length && text[i] == '(')
            {
                i++;
                var start = i;
                while (i < text.Length && text[i] != ')') i++;
                typeKeyword = text.Substring(start, Math.Max(0, i - start)).Trim().ToLowerInvariant();
                if (i < text.Length) i++;
            }

            SkipSpaces(text, ref i);
            if (i < text.Length && (text[i] == '"' || text[i] == '\''))
                title = ReadQuoted(text, ref i);

            if (string.IsNullOrWhiteSpace(name))
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    graph.Warnings.Add($"Line {lineNumber}: could not parse \"{text}\".");
                    return null;
                }
                name = Slug(title);
            }

            if (string.IsNullOrWhiteSpace(title))
                title = Prettify(name);

            var existing = graph.Find(name);
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(typeKeyword) && typeKeyword != existing.TypeKeyword)
                {
                    graph.Warnings.Add($"Line {lineNumber}: node '{name}' already declared as '{existing.TypeKeyword}'; keeping the first type.");
                }
                if (title != existing.Title && title != Prettify(existing.Name))
                    existing.Title = title;
                return existing;
            }

            var keyword = NormalizeType(typeKeyword) ?? InferType(name);
            if (!TypeMap.TryGetValue(keyword, out var info))
            {
                if (!string.IsNullOrWhiteSpace(typeKeyword))
                    graph.Warnings.Add($"Line {lineNumber}: unknown node type '{typeKeyword}' for '{name}'; using process.");
                keyword = "process";
                info = TypeMap["process"];
            }

            if ((keyword == "start" || keyword == "end") &&
                string.Equals(title, Prettify(name), StringComparison.OrdinalIgnoreCase))
            {
                title = keyword == "start" ? "Start" : "End";
            }

            var node = new DslNode
            {
                Name = name,
                TypeKeyword = keyword,
                ClassName = info.Class,
                Title = title,
                Width = info.W,
                Height = info.H
            };
            graph.Nodes.Add(node);
            return node;
        }

        private static string NormalizeType(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;
            var normalized = new string(keyword.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            return normalized.Length == 0 ? null : normalized;
        }

        private static string InferType(string name)
        {
            var key = new string((name ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            switch (key)
            {
                case "start":
                case "begin":
                case "entry":
                    return "start";
                case "end":
                case "stop":
                case "finish":
                case "done":
                case "exit":
                    return "end";
                default:
                    return "process";
            }
        }

        /// <summary>Assigns ranks (top-down depth) and row/column positions.</summary>
        public static void Layout(DslGraph graph)
        {
            if (graph == null || graph.Nodes.Count == 0) return;

            var indegree = graph.Nodes.ToDictionary(n => n.Name, _ => 0, StringComparer.OrdinalIgnoreCase);
            foreach (var edge in graph.Edges)
            {
                if (indegree.ContainsKey(edge.To)) indegree[edge.To]++;
            }

            var rank = graph.Nodes.ToDictionary(n => n.Name, _ => 0, StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<DslNode>(graph.Nodes.Where(n => indegree[n.Name] == 0));
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!processed.Add(node.Name)) continue;

                foreach (var edge in graph.Edges.Where(e => string.Equals(e.From, node.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    var target = graph.Find(edge.To);
                    if (target == null) continue;
                    rank[target.Name] = Math.Max(rank[target.Name], rank[node.Name] + 1);
                    if (--indegree[target.Name] <= 0) queue.Enqueue(target);
                }
            }

            var maxRank = rank.Values.DefaultIfEmpty(0).Max();
            foreach (var node in graph.Nodes.Where(n => !processed.Contains(n.Name)))
            {
                maxRank++;
                rank[node.Name] = maxRank;
                graph.Warnings.Add($"Cycle detected at '{node.Name}'; its position is approximate.");
            }

            foreach (var node in graph.Nodes) node.Rank = rank[node.Name];

            const float marginX = 60f;
            const float marginY = 40f;
            const float hGap = 60f;
            const float vGap = 110f;

            var rows = graph.Nodes.GroupBy(n => n.Rank).OrderBy(g => g.Key).ToList();
            var rowWidths = rows.ToDictionary(g => g.Key, g => g.Sum(n => n.Width) + hGap * Math.Max(0, g.Count() - 1));
            var maxRowWidth = rowWidths.Values.DefaultIfEmpty(0f).Max();

            foreach (var row in rows)
            {
                var x = marginX + (maxRowWidth - rowWidths[row.Key]) / 2f;
                var y = marginY + row.Key * vGap;
                foreach (var node in row)
                {
                    node.X = x;
                    node.Y = y;
                    x += node.Width + hGap;
                }
            }
        }

        // ── Small parsing helpers ────────────────────────────────────────────

        private static (string Body, string Label) SplitTrailingLabel(string line)
        {
            var inQuote = '\0';
            var lastColon = -1;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuote != '\0')
                {
                    if (c == inQuote) inQuote = '\0';
                    continue;
                }
                if (c == '"' || c == '\'') { inQuote = c; continue; }
                if (c == ':') lastColon = i;
            }

            if (lastColon < 0) return (line, null);
            var label = line.Substring(lastColon + 1).Trim();
            return (line.Substring(0, lastColon).Trim(), label.Length == 0 ? null : label);
        }

        private static List<string> SplitTopLevel(string text, string separator)
        {
            var parts = new List<string>();
            var inQuote = '\0';
            var start = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuote != '\0')
                {
                    if (c == inQuote) inQuote = '\0';
                    continue;
                }
                if (c == '"' || c == '\'') { inQuote = c; continue; }

                if (c == separator[0] && i + separator.Length <= text.Length &&
                    string.CompareOrdinal(text, i, separator, 0, separator.Length) == 0)
                {
                    parts.Add(text.Substring(start, i - start));
                    i += separator.Length - 1;
                    start = i + 1;
                }
            }

            parts.Add(text.Substring(start));
            return parts;
        }

        private static string ReadQuoted(string text, ref int index)
        {
            var quote = text[index];
            index++;
            var builder = new StringBuilder();
            while (index < text.Length && text[index] != quote)
            {
                builder.Append(text[index]);
                index++;
            }
            if (index < text.Length) index++;
            return builder.ToString().Trim();
        }

        private static void SkipSpaces(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        }

        private static string Slug(string value)
        {
            var cleaned = new string((value ?? string.Empty)
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray()).Trim('_');
            return cleaned.Length == 0 ? "node" : cleaned;
        }

        private static string Prettify(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var spaced = name.Replace('_', ' ').Replace('-', ' ');
            return string.Join(" ", spaced.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
