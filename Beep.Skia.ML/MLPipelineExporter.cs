using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Beep.Skia.Model;

namespace Beep.Skia.ML
{
    /// <summary>
    /// A node in an exported ML pipeline definition.
    /// </summary>
    public class MLPipelineNode
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// A directed edge in an exported ML pipeline definition.
    /// </summary>
    public class MLPipelineEdge
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string? Label { get; set; }
    }

    /// <summary>
    /// Portable ML pipeline definition (nodes in execution order + edges).
    /// Consumable by external frameworks (Python, ONNX tooling, orchestration engines).
    /// </summary>
    public class MLPipelineDefinition
    {
        public string Name { get; set; } = "Pipeline";
        public string Version { get; set; } = "1.0";
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        public List<MLPipelineNode> Nodes { get; set; } = new List<MLPipelineNode>();
        public List<MLPipelineEdge> Edges { get; set; } = new List<MLPipelineEdge>();
    }

    /// <summary>
    /// Exports ML pipeline diagrams to a portable JSON definition:
    /// nodes are topologically ordered and carry their node properties (hyperparameters),
    /// edges carry the connection labels.
    /// </summary>
    public class MLPipelineExporter
    {
        /// <summary>
        /// Builds the pipeline definition from diagram components and lines.
        /// </summary>
        public MLPipelineDefinition BuildDefinition(
            IReadOnlyList<SkiaComponent> components,
            IReadOnlyList<IConnectionLine> lines,
            string pipelineName = "Pipeline")
        {
            var definition = new MLPipelineDefinition { Name = pipelineName ?? "Pipeline" };

            var nodes = (components ?? Array.Empty<SkiaComponent>())
                .Where(c => c is MLControl && !c.IsStatic)
                .ToList();
            if (nodes.Count == 0) return definition;

            var idOf = new Dictionary<SkiaComponent, string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                idOf[nodes[i]] = $"node_{i + 1}";
            }

            // Build edges between ML nodes.
            var outgoing = nodes.ToDictionary(n => n, _ => new List<SkiaComponent>());
            var inDegree = nodes.ToDictionary(n => n, _ => 0);

            foreach (var line in lines ?? Array.Empty<IConnectionLine>())
            {
                if (!(line?.Start?.Component is SkiaComponent source)) continue;
                if (!(line?.End?.Component is SkiaComponent target)) continue;
                if (!idOf.ContainsKey(source) || !idOf.ContainsKey(target)) continue;
                if (ReferenceEquals(source, target)) continue;

                outgoing[source].Add(target);
                inDegree[target]++;

                definition.Edges.Add(new MLPipelineEdge
                {
                    From = idOf[source],
                    To = idOf[target],
                    Label = string.IsNullOrWhiteSpace(line.Label1) ? null : line.Label1
                });
            }

            // Kahn topological sort; cycles are appended in declaration order.
            var queue = new Queue<SkiaComponent>(nodes.Where(n => inDegree[n] == 0));
            var ordered = new List<SkiaComponent>();
            var remaining = new HashSet<SkiaComponent>(nodes);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                ordered.Add(node);
                remaining.Remove(node);

                foreach (var successor in outgoing[node])
                {
                    inDegree[successor]--;
                    if (inDegree[successor] == 0) queue.Enqueue(successor);
                }
            }
            ordered.AddRange(nodes.Where(n => remaining.Contains(n)));

            for (int i = 0; i < ordered.Count; i++)
            {
                var node = ordered[i];
                definition.Nodes.Add(new MLPipelineNode
                {
                    Id = idOf[node],
                    Type = node.GetType().Name,
                    Name = node.Name ?? string.Empty,
                    Order = i,
                    Properties = CollectProperties(node)
                });
            }

            return definition;
        }

        /// <summary>
        /// Serializes the pipeline definition to JSON.
        /// </summary>
        public string ExportJson(
            IReadOnlyList<SkiaComponent> components,
            IReadOnlyList<IConnectionLine> lines,
            string pipelineName = "Pipeline",
            bool indented = true)
        {
            var definition = BuildDefinition(components, lines, pipelineName);
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(definition, options);
        }

        /// <summary>
        /// Serializes the ML pipeline held by a drawing manager.
        /// </summary>
        public string ExportJson(DrawingManager manager, string pipelineName = "Pipeline", bool indented = true)
        {
            if (manager == null) return "{}";
            return ExportJson(manager.GetComponents(), manager.GetLines(), pipelineName, indented);
        }

        private static Dictionary<string, object> CollectProperties(SkiaComponent node)
        {
            var result = new Dictionary<string, object>();
            try
            {
                foreach (var kvp in node.GetProperties(includeCommon: false, includeNodeProperties: true))
                {
                    if (kvp.Value == null) continue;
                    var normalized = Normalize(kvp.Value);
                    if (normalized != null) result[kvp.Key] = normalized;
                }
            }
            catch { }
            return result;
        }

        private static object? Normalize(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string _:
                case bool _:
                case int _:
                case long _:
                case double _:
                case float _:
                case decimal _:
                    return value;
                default:
                    try { return Convert.ToString(value, CultureInfo.InvariantCulture); }
                    catch { return value.ToString(); }
            }
        }
    }
}