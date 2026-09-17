using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beep.Skia.Model;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Target languages for <see cref="FlowchartCodeGenerator"/>.
    /// </summary>
    public enum CodeLanguage
    {
        /// <summary>Language-neutral structured pseudocode.</summary>
        Pseudocode,
        /// <summary>Python 3 source.</summary>
        Python,
        /// <summary>C# source.</summary>
        CSharp
    }

    /// <summary>
    /// Generates structured code from a flowchart diagram.
    /// Conventions: for decision nodes the first output port is the true branch and the second is the false branch;
    /// for loop nodes the first output port is the loop body and the second is the exit.
    /// </summary>
    public class FlowchartCodeGenerator
    {
        private const int MaxDepth = 64;

        private sealed class Edge
        {
            public required IConnectionLine Line;
            public required SkiaComponent Target;
            public int PortIndex;
        }

        /// <summary>
        /// Generates code for the flowchart held by a drawing manager.
        /// </summary>
        public string Generate(DrawingManager manager, CodeLanguage language = CodeLanguage.Pseudocode)
        {
            if (manager == null) return string.Empty;
            return Generate(manager.GetComponents(), manager.GetLines(), language);
        }

        /// <summary>
        /// Generates code for the given components and connection lines.
        /// </summary>
        public string Generate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines, CodeLanguage language = CodeLanguage.Pseudocode)
        {
            if (components == null || components.Count == 0) return string.Empty;

            var nodes = components
                .Where(c => c is FlowchartControl && !(c is AnnotationNode) && !c.IsStatic)
                .Cast<FlowchartControl>()
                .ToList();

            if (nodes.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            EmitHeader(sb, language);

            var start = FindEntry(nodes, lines);
            var path = new HashSet<SkiaComponent>();
            if (start != null)
            {
                EmitNode(start, 1, path, sb, language, lines, 0);
            }
            else
            {
                AppendLine(sb, 1, Comment("(no start node found)", language), language);
            }

            EmitFooter(sb, language);
            return sb.ToString();
        }

        private FlowchartControl? FindEntry(List<FlowchartControl> nodes, IReadOnlyList<IConnectionLine> lines)
        {
            var starts = nodes.OfType<StartEndNode>().ToList();
            if (starts.Count > 0)
            {
                var noIncoming = starts.FirstOrDefault(s => !HasIncoming(s, lines));
                return noIncoming ?? starts.First();
            }

            return nodes.FirstOrDefault(n => !HasIncoming(n, lines)) ?? nodes.FirstOrDefault();
        }

        private static bool HasIncoming(SkiaComponent node, IReadOnlyList<IConnectionLine> lines)
        {
            if (lines == null) return false;
            return lines.Any(l => l?.End?.Component == node);
        }

        private void EmitNode(SkiaComponent? node, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            if (node == null || depth > MaxDepth) return;

            if (path.Contains(node))
            {
                AppendLine(sb, indent, Comment($"loop back to {GetLabel(node)}", language), language);
                return;
            }

            path.Add(node);
            try
            {
                switch (node)
                {
                    case StartEndNode _:
                        if (IsEndNode(node)) { AppendLine(sb, indent, ReturnStatement(language), language); return; }
                        break; // start: continue to successors

                    case DecisionNode decision:
                        EmitDecision(decision, indent, path, sb, language, lines, depth);
                        return;

                    case WhileLoopNode whileNode:
                        EmitWhile(whileNode, indent, path, sb, language, lines, depth);
                        return;

                    case DoWhileLoopNode doWhile:
                        EmitDoWhile(doWhile, indent, path, sb, language, lines, depth);
                        return;

                    case ForLoopNode forLoop:
                        EmitFor(forLoop, indent, path, sb, language, lines, depth);
                        return;

                    case ForkNode fork:
                        EmitFork(fork, indent, path, sb, language, lines, depth);
                        return;

                    case MergeNode _:
                    case JoinNode _:
                        break; // pass-through

                    case AnnotationNode _:
                    case CommentNode _:
                        AppendLine(sb, indent, Comment(GetLabel(node) ?? "(note)", language), language);
                        break;

                    case ConnectorNode connector:
                        AppendLine(sb, indent, Comment($"connector {connector.ConnectorId}", language), language);
                        break;

                    case OffPageReferenceNode offPage:
                        AppendLine(sb, indent, Comment($"off-page reference {offPage.ReferenceId} (page {offPage.PageNumber})", language), language);
                        break;

                    case SubProcessNode sub:
                        AppendLine(sb, indent, Statement($"call {GetLabel(sub)}", language), language);
                        break;

                    case DelayNode delay:
                        var delayLabel = GetLabel(delay);
                        AppendLine(sb, indent, Statement(string.IsNullOrWhiteSpace(delay.Duration)
                            ? $"delay {delayLabel}"
                            : $"delay {delay.Duration} ({delayLabel})", language), language);
                        break;

                    default:
                        var label = GetLabel(node);
                        if (!string.IsNullOrWhiteSpace(label))
                            AppendLine(sb, indent, Statement(label, language), language);
                        break;
                }

                var next = GetOutgoing(node, lines).FirstOrDefault();
                EmitNode(next?.Target, indent, path, sb, language, lines, depth + 1);
            }
            finally
            {
                path.Remove(node);
            }
        }

        private void EmitDecision(DecisionNode node, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            var outs = GetOutgoing(node, lines);
            var trueEdge = outs.FirstOrDefault(e => e.PortIndex == 0) ?? outs.ElementAtOrDefault(0);
            var falseEdge = outs.FirstOrDefault(e => e.PortIndex == 1) ?? outs.ElementAtOrDefault(1);
            var condition = GetLabel(node) ?? "condition";

            switch (language)
            {
                case CodeLanguage.Python:
                    AppendLine(sb, indent, $"if {condition}:", language);
                    EmitBranch(trueEdge, indent + 1, path, sb, language, lines, depth);
                    if (ShouldEmitElse(trueEdge, falseEdge))
                    {
                        AppendLine(sb, indent, "else:", language);
                        EmitBranch(falseEdge, indent + 1, path, sb, language, lines, depth);
                    }
                    break;
                case CodeLanguage.CSharp:
                    AppendLine(sb, indent, $"if ({condition})", language);
                    AppendLine(sb, indent, "{", language);
                    EmitBranch(trueEdge, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, "}", language);
                    if (ShouldEmitElse(trueEdge, falseEdge))
                    {
                        AppendLine(sb, indent, "else", language);
                        AppendLine(sb, indent, "{", language);
                        EmitBranch(falseEdge, indent + 1, path, sb, language, lines, depth);
                        AppendLine(sb, indent, "}", language);
                    }
                    break;
                default:
                    AppendLine(sb, indent, $"IF {condition} THEN", language);
                    EmitBranch(trueEdge, indent + 1, path, sb, language, lines, depth);
                    if (ShouldEmitElse(trueEdge, falseEdge))
                    {
                        AppendLine(sb, indent, "ELSE", language);
                        EmitBranch(falseEdge, indent + 1, path, sb, language, lines, depth);
                    }
                    AppendLine(sb, indent, "END IF", language);
                    break;
            }
        }

        private static bool ShouldEmitElse(Edge? trueEdge, Edge? falseEdge)
        {
            if (falseEdge?.Target == null) return false;
            // Both branches converging on the same merge target: emit only the true branch.
            if (trueEdge?.Target == falseEdge.Target) return false;
            return true;
        }

        private void EmitWhile(WhileLoopNode node, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            var outs = GetOutgoing(node, lines);
            var body = outs.FirstOrDefault(e => e.PortIndex == 0) ?? outs.ElementAtOrDefault(0);
            var exit = outs.FirstOrDefault(e => e.PortIndex == 1) ?? outs.ElementAtOrDefault(1);
            var condition = node.Condition ?? "condition";

            switch (language)
            {
                case CodeLanguage.Python:
                    AppendLine(sb, indent, $"while {condition}:", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    break;
                case CodeLanguage.CSharp:
                    AppendLine(sb, indent, $"while ({condition})", language);
                    AppendLine(sb, indent, "{", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, "}", language);
                    break;
                default:
                    AppendLine(sb, indent, $"WHILE {condition} DO", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, "END WHILE", language);
                    break;
            }

            EmitNode(exit?.Target, indent, path, sb, language, lines, depth + 1);
        }

        private void EmitDoWhile(DoWhileLoopNode node, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            var outs = GetOutgoing(node, lines);
            var body = outs.FirstOrDefault(e => e.PortIndex == 0) ?? outs.ElementAtOrDefault(0);
            var exit = outs.FirstOrDefault(e => e.PortIndex == 1) ?? outs.ElementAtOrDefault(1);
            var condition = node.Condition ?? "condition";

            switch (language)
            {
                case CodeLanguage.Python:
                    AppendLine(sb, indent, "while True:", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent + 1, $"if not ({condition}): break", language);
                    break;
                case CodeLanguage.CSharp:
                    AppendLine(sb, indent, "do", language);
                    AppendLine(sb, indent, "{", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, $"}} while ({condition});", language);
                    break;
                default:
                    AppendLine(sb, indent, "REPEAT", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, $"UNTIL NOT ({condition})", language);
                    break;
            }

            EmitNode(exit?.Target, indent, path, sb, language, lines, depth + 1);
        }

        private void EmitFor(ForLoopNode node, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            var outs = GetOutgoing(node, lines);
            var body = outs.FirstOrDefault(e => e.PortIndex == 0) ?? outs.ElementAtOrDefault(0);
            var exit = outs.FirstOrDefault(e => e.PortIndex == 1) ?? outs.ElementAtOrDefault(1);

            var init = string.IsNullOrWhiteSpace(node.InitExpression)
                ? $"{(string.IsNullOrWhiteSpace(node.LoopVariable) ? "i" : node.LoopVariable)} = 0"
                : node.InitExpression;
            var condition = string.IsNullOrWhiteSpace(node.Condition) ? "condition" : node.Condition;
            var increment = string.IsNullOrWhiteSpace(node.Increment) ? "step" : node.Increment;

            switch (language)
            {
                case CodeLanguage.Python:
                    AppendLine(sb, indent, $"# for {node.LoopVariable}", language);
                    AppendLine(sb, indent, $"{init}", language);
                    AppendLine(sb, indent, $"while {condition}:", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent + 1, increment, language);
                    break;
                case CodeLanguage.CSharp:
                    AppendLine(sb, indent, $"for ({init}; {condition}; {increment})", language);
                    AppendLine(sb, indent, "{", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, "}", language);
                    break;
                default:
                    AppendLine(sb, indent, $"FOR {init} WHILE {condition} STEP {increment} DO", language);
                    EmitBranch(body, indent + 1, path, sb, language, lines, depth);
                    AppendLine(sb, indent, "END FOR", language);
                    break;
            }

            EmitNode(exit?.Target, indent, path, sb, language, lines, depth + 1);
        }

        private void EmitFork(ForkNode node, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            var outs = GetOutgoing(node, lines);
            AppendLine(sb, indent, Comment($"parallel split ({Math.Max(node.ParallelPaths, outs.Count)} paths)", language), language);
            foreach (var edge in outs)
            {
                EmitNode(edge.Target, indent, path, sb, language, lines, depth + 1);
            }
        }

        private void EmitBranch(Edge? edge, int indent, HashSet<SkiaComponent> path, StringBuilder sb, CodeLanguage language, IReadOnlyList<IConnectionLine> lines, int depth)
        {
            if (edge?.Target == null)
            {
                AppendLine(sb, indent, NoOpStatement(language), language);
                return;
            }

            EmitNode(edge.Target, indent, path, sb, language, lines, depth + 1);
        }

        private static List<Edge> GetOutgoing(SkiaComponent node, IReadOnlyList<IConnectionLine> lines)
        {
            var result = new List<Edge>();
            if (node == null || lines == null) return result;

            foreach (var line in lines)
            {
                if (line == null) continue;
                if (line.Start?.Component == node && line.End?.Component is SkiaComponent target)
                {
                    result.Add(new Edge
                    {
                        Line = line,
                        Target = target,
                        PortIndex = GetPortIndex(node, line.Start)
                    });
                }
            }

            return result
                .OrderBy(e => e.PortIndex)
                .ThenBy(e => e.Target.Y)
                .ThenBy(e => e.Target.X)
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

        private static bool IsEndNode(SkiaComponent node)
        {
            if (!(node is StartEndNode)) return false;
            var label = GetLabel(node);
            if (!string.IsNullOrWhiteSpace(label) &&
                label.IndexOf("end", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // An end terminator has no outgoing edges; a start terminator does.
            return node.OutgoingConnections == null || node.OutgoingConnections.Count == 0;
        }

        private static string? GetLabel(SkiaComponent? node)
        {
            if (node == null) return null;

            foreach (var name in new[] { "Label", "Text", "Condition", "Title", "TaskName", "ActionName", "SubProcessId" })
            {
                var prop = node.GetType().GetProperty(name);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(node) as string;
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }
            }

            if (!string.IsNullOrWhiteSpace(node.DisplayText)) return node.DisplayText;
            return node.Name;
        }

        private static void EmitHeader(StringBuilder sb, CodeLanguage language)
        {
            switch (language)
            {
                case CodeLanguage.Python:
                    sb.AppendLine("def main():");
                    break;
                case CodeLanguage.CSharp:
                    sb.AppendLine("void Main()");
                    sb.AppendLine("{");
                    break;
                default:
                    sb.AppendLine("BEGIN");
                    break;
            }
        }

        private static void EmitFooter(StringBuilder sb, CodeLanguage language)
        {
            switch (language)
            {
                case CodeLanguage.CSharp:
                    sb.AppendLine("}");
                    break;
                case CodeLanguage.Pseudocode:
                    sb.AppendLine("END");
                    break;
            }
        }

        private static string NoOpStatement(CodeLanguage language) => language switch
        {
            CodeLanguage.Python => "pass",
            CodeLanguage.CSharp => ";",
            _ => "NOP"
        };

        private static string ReturnStatement(CodeLanguage language) => language switch
        {
            CodeLanguage.Python => "return",
            CodeLanguage.CSharp => "return;",
            _ => "END"
        };

        private static string Statement(string label, CodeLanguage language)
        {
            var text = (label ?? string.Empty).Trim().TrimEnd(';');
            return language == CodeLanguage.Python ? text : text + ";";
        }

        private static string Comment(string text, CodeLanguage language) => language switch
        {
            CodeLanguage.Python => $"# {text}",
            CodeLanguage.CSharp => $"// {text}",
            _ => $"-- {text}"
        };

        private static string Indent(int level, CodeLanguage language)
            => new string(' ', Math.Max(0, level) * 4);

        private static void AppendLine(StringBuilder sb, int indent, string text, CodeLanguage language)
            => sb.AppendLine(Indent(indent, language) + text);
    }
}