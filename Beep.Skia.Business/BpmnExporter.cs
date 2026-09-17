using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkiaSharp;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Exports a Business Process diagram as BPMN 2.0 XML (semantic model + BPMN DI).
    ///
    /// Without pools, a single bpmn:process is emitted (sequence flows only).
    /// With <see cref="BpmnPoolNode"/> pools, a bpmn:collaboration is emitted with one
    /// participant + process per pool; nodes are assigned to pools by geometric containment;
    /// <see cref="BpmnMessageFlow"/> lines (or lines crossing pools) become bpmn:messageFlow.
    /// <see cref="BpmnLaneNode"/> lanes become bpmn:laneSet/bpmn:lane with flowNodeRefs.
    /// </summary>
    public class BpmnExporter
    {
        private const float PoolTitleBarWidth = 26f;

        /// <summary>
        /// Exports components and lines to BPMN 2.0 XML.
        /// </summary>
        public string Export(IReadOnlyList<SkiaComponent> components, IReadOnlyList<Model.IConnectionLine> lines, string processName = "Process")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<bpmn:definitions xmlns:bpmn=\"http://www.omg.org/spec/BPMN/20100524/MODEL\"");
            sb.AppendLine("  xmlns:bpmndi=\"http://www.omg.org/spec/BPMN/20100524/DI\"");
            sb.AppendLine("  xmlns:dc=\"http://www.omg.org/spec/DD/20100524/DC\"");
            sb.AppendLine("  xmlns:di=\"http://www.omg.org/spec/DD/20100524/DI\"");
            sb.AppendLine("  xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            sb.AppendLine("  targetNamespace=\"http://bpmn.io/schema/bpmn\">");

            var all = (components ?? Array.Empty<SkiaComponent>()).Where(c => c != null && !c.IsStatic).ToList();
            var pools = all.OfType<BpmnPoolNode>().ToList();
            var lanes = all.OfType<BpmnLaneNode>().ToList();
            var nodes = all
                .Where(c => c is BusinessControl && !(c is BpmnPoolNode) && !(c is BpmnLaneNode))
                .Cast<BusinessControl>()
                .ToList();

            var idCounter = 0;
            string NextId(string prefix) => $"{prefix}_{++idCounter}";
            var shapes = new List<(string Id, SkiaComponent Node)>();
            var edges = new List<(string FlowId, Model.IConnectionLine Line)>();

            // Pre-assign element ids so flows can reference them regardless of emission order.
            foreach (var node in nodes)
            {
                string id = NextId(GetBpmnPrefix(node));
                node.Tag = id;
                shapes.Add((id, node));
            }

            if (pools.Count == 0)
            {
                ExportSingleProcess(sb, nodes, lanes, lines, processName, NextId, edges);
            }
            else
            {
                ExportCollaboration(sb, nodes, pools, lanes, lines, processName, NextId, edges);
            }

            AppendDiagramInterchange(sb, processName, shapes, edges);

            sb.AppendLine("</bpmn:definitions>");
            return sb.ToString();
        }

        // ── Single process (no pools) ────────────────────────────────────────

        private void ExportSingleProcess(
            StringBuilder sb,
            List<BusinessControl> nodes,
            List<BpmnLaneNode> lanes,
            IReadOnlyList<Model.IConnectionLine> lines,
            string processName,
            Func<string, string> nextId,
            List<(string, Model.IConnectionLine)> edges)
        {
            sb.AppendLine($"  <bpmn:process id=\"{EscapeXml(processName)}\" name=\"{EscapeXml(processName)}\">");

            if (lanes.Count > 0)
            {
                AppendLaneSet(sb, "    ", lanes, nodes, nextId);
            }

            EmitElements(sb, "    ", nodes);

            foreach (var line in lines ?? Array.Empty<Model.IConnectionLine>())
            {
                var src = line?.Start?.Component as SkiaComponent;
                var dst = line?.End?.Component as SkiaComponent;
                if (src?.Tag is string srcId && dst?.Tag is string dstId)
                {
                    string flowId = nextId("Flow");
                    edges.Add((flowId, line));
                    string label = !string.IsNullOrWhiteSpace(line.Label1) ? $" name=\"{EscapeXml(line.Label1)}\"" : "";
                    sb.AppendLine($"    <bpmn:sequenceFlow id=\"{flowId}\" sourceRef=\"{srcId}\" targetRef=\"{dstId}\"{label} />");
                }
            }

            sb.AppendLine("  </bpmn:process>");
        }

        // ── Collaboration (pools) ────────────────────────────────────────────

        private void ExportCollaboration(
            StringBuilder sb,
            List<BusinessControl> nodes,
            List<BpmnPoolNode> pools,
            List<BpmnLaneNode> lanes,
            IReadOnlyList<Model.IConnectionLine> lines,
            string processName,
            Func<string, string> nextId,
            List<(string, Model.IConnectionLine)> edges)
        {
            // Assign nodes and lanes to pools by center containment (first match wins).
            var nodePool = new Dictionary<BusinessControl, BpmnPoolNode>();
            foreach (var node in nodes)
            {
                var pool = pools.FirstOrDefault(p => ContainsCenter(p, node));
                if (pool != null) nodePool[node] = pool;
            }

            var lanePool = new Dictionary<BpmnLaneNode, BpmnPoolNode>();
            foreach (var lane in lanes)
            {
                var pool = pools.FirstOrDefault(p => ContainsCenter(p, lane));
                if (pool != null) lanePool[lane] = pool;
            }

            // One process per pool (plus a default process for unassigned nodes).
            var poolProcessId = new Dictionary<BpmnPoolNode, string>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pool in pools)
            {
                var baseName = string.IsNullOrWhiteSpace(pool.PoolName) ? "Pool" : pool.PoolName;
                var candidate = "Process_" + Sanitize(baseName);
                var unique = candidate;
                int suffix = 2;
                while (!usedNames.Add(unique)) unique = candidate + "_" + suffix++;
                poolProcessId[pool] = unique;
            }

            bool hasUnassigned = nodes.Any(n => !nodePool.ContainsKey(n));
            string defaultProcessId = hasUnassigned ? Sanitize(processName) : null;
            if (hasUnassigned) usedNames.Add(defaultProcessId);

            // Collaboration with participants.
            sb.AppendLine("  <bpmn:collaboration id=\"Collaboration_1\">");
            var participantIds = new Dictionary<BpmnPoolNode, string>();
            foreach (var pool in pools)
            {
                string participantId = nextId("Participant");
                participantIds[pool] = participantId;
                sb.AppendLine($"    <bpmn:participant id=\"{participantId}\" name=\"{EscapeXml(pool.PoolName)}\" processRef=\"{EscapeXml(poolProcessId[pool])}\" />");
            }

            // Classify lines: message flows cross participants (or are explicit BpmnMessageFlow).
            var sequenceFlows = new List<(string FlowId, Model.IConnectionLine Line, BpmnPoolNode Pool)>();
            var messageFlows = new List<(string FlowId, Model.IConnectionLine Line)>();

            foreach (var line in lines ?? Array.Empty<Model.IConnectionLine>())
            {
                var src = line?.Start?.Component as SkiaComponent;
                var dst = line?.End?.Component as SkiaComponent;
                if (!(src is BusinessControl srcNode) || !(dst is BusinessControl dstNode)) continue;

                nodePool.TryGetValue(srcNode, out var srcPool);
                nodePool.TryGetValue(dstNode, out var dstPool);

                bool explicitMessage = line is BpmnMessageFlow;
                bool crossesPools = srcPool != null && dstPool != null && !ReferenceEquals(srcPool, dstPool);

                if (explicitMessage || crossesPools)
                {
                    messageFlows.Add((nextId("MessageFlow"), line));
                }
                else
                {
                    sequenceFlows.Add((nextId("Flow"), line, srcPool));
                }
            }

            foreach (var (flowId, line) in messageFlows)
            {
                var srcId = (line.Start?.Component as SkiaComponent)?.Tag as string;
                var dstId = (line.End?.Component as SkiaComponent)?.Tag as string;
                string label = !string.IsNullOrWhiteSpace(line.Label1) ? $" name=\"{EscapeXml(line.Label1)}\"" : "";
                sb.AppendLine($"    <bpmn:messageFlow id=\"{flowId}\" sourceRef=\"{srcId}\" targetRef=\"{dstId}\"{label} />");
                edges.Add((flowId, line));
            }

            sb.AppendLine("  </bpmn:collaboration>");

            // Processes per pool.
            foreach (var pool in pools)
            {
                var poolNodes = nodes.Where(n => nodePool.TryGetValue(n, out var p) && ReferenceEquals(p, pool)).ToList();
                sb.AppendLine($"  <bpmn:process id=\"{EscapeXml(poolProcessId[pool])}\" name=\"{EscapeXml(pool.PoolName)}\">");

                var poolLanes = lanes.Where(l => lanePool.TryGetValue(l, out var p) && ReferenceEquals(p, pool)).ToList();
                if (poolLanes.Count > 0) AppendLaneSet(sb, "    ", poolLanes, poolNodes, nextId);

                EmitElements(sb, "    ", poolNodes);

                foreach (var (flowId, line, flowPool) in sequenceFlows)
                {
                    var src = line.Start?.Component as SkiaComponent;
                    var dst = line.End?.Component as SkiaComponent;
                    if (src?.Tag is string srcId && dst?.Tag is string dstId)
                    {
                        string label = !string.IsNullOrWhiteSpace(line.Label1) ? $" name=\"{EscapeXml(line.Label1)}\"" : "";
                        sb.AppendLine($"    <bpmn:sequenceFlow id=\"{flowId}\" sourceRef=\"{srcId}\" targetRef=\"{dstId}\"{label} />");
                    }
                }

                sb.AppendLine("  </bpmn:process>");
            }

            // Default process for nodes outside any pool.
            if (hasUnassigned)
            {
                var unassigned = nodes.Where(n => !nodePool.ContainsKey(n)).ToList();
                sb.AppendLine($"  <bpmn:process id=\"{EscapeXml(defaultProcessId)}\" name=\"{EscapeXml(processName)}\">");
                EmitElements(sb, "    ", unassigned);
                foreach (var (flowId, line, flowPool) in sequenceFlows.Where(f => f.Pool == null))
                {
                    var src = line.Start?.Component as SkiaComponent;
                    var dst = line.End?.Component as SkiaComponent;
                    if (src?.Tag is string srcId && dst?.Tag is string dstId)
                    {
                        string label = !string.IsNullOrWhiteSpace(line.Label1) ? $" name=\"{EscapeXml(line.Label1)}\"" : "";
                        sb.AppendLine($"    <bpmn:sequenceFlow id=\"{flowId}\" sourceRef=\"{srcId}\" targetRef=\"{dstId}\"{label} />");
                    }
                }
                sb.AppendLine("  </bpmn:process>");
            }
        }

        // ── Shared emission helpers ──────────────────────────────────────────

        private void EmitElements(StringBuilder sb, string indent, List<BusinessControl> nodes)
        {
            foreach (var node in nodes)
            {
                if (!(node.Tag is string id)) continue;

                switch (node)
                {
                    case StartEvent _:
                        sb.AppendLine($"{indent}<bpmn:startEvent id=\"{id}\" name=\"{EscapeXml(node.Name)}\" />");
                        break;
                    case EndEvent _:
                        sb.AppendLine($"{indent}<bpmn:endEvent id=\"{id}\" name=\"{EscapeXml(node.Name)}\" />");
                        break;
                    case IntermediateEventNode intEvent:
                        var catchThrow = intEvent.EventPosition == EventPosition.IntermediateThrow ? "intermediateThrowEvent" : "intermediateCatchEvent";
                        sb.AppendLine($"{indent}<bpmn:{catchThrow} id=\"{id}\" name=\"{EscapeXml(intEvent.Label)}\">");
                        sb.AppendLine($"{indent}  <bpmn:{GetEventTypeElement(intEvent.EventType)} />");
                        sb.AppendLine($"{indent}</bpmn:{catchThrow}>");
                        break;
                    case Gateway gw:
                        sb.AppendLine($"{indent}<bpmn:{GetGatewayElement(gw.GatewayType)} id=\"{id}\" name=\"{EscapeXml(gw.Name)}\" />");
                        break;
                    case BusinessTask task:
                        sb.AppendLine($"{indent}<bpmn:{GetTaskElement(task.TaskType)} id=\"{id}\" name=\"{EscapeXml(task.Label)}\" />");
                        break;
                    default:
                        sb.AppendLine($"{indent}<bpmn:task id=\"{id}\" name=\"{EscapeXml(node.Name)}\" />");
                        break;
                }
            }
        }

        private static void AppendLaneSet(
            StringBuilder sb,
            string indent,
            List<BpmnLaneNode> lanes,
            List<BusinessControl> nodes,
            Func<string, string> nextId)
        {
            sb.AppendLine($"{indent}<bpmn:laneSet id=\"{nextId("LaneSet")}\">");
            foreach (var lane in lanes)
            {
                string laneId = nextId("Lane");
                sb.AppendLine($"{indent}  <bpmn:lane id=\"{laneId}\" name=\"{EscapeXml(lane.LaneName)}\">");

                var contained = nodes.Where(n => ContainsCenter(lane, n)).ToList();
                foreach (var node in contained)
                {
                    if (node.Tag is string nodeId)
                        sb.AppendLine($"{indent}    <bpmn:flowNodeRef>{EscapeXml(nodeId)}</bpmn:flowNodeRef>");
                }

                sb.AppendLine($"{indent}  </bpmn:lane>");
            }
            sb.AppendLine($"{indent}</bpmn:laneSet>");
        }

        private static void AppendDiagramInterchange(
            StringBuilder sb,
            string processName,
            List<(string Id, SkiaComponent Node)> shapes,
            List<(string FlowId, Model.IConnectionLine Line)> edges)
        {
            sb.AppendLine("  <bpmndi:BPMNDiagram id=\"BPMNDiagram_1\">");
            sb.AppendLine($"    <bpmndi:BPMNPlane id=\"BPMNPlane_1\" bpmnElement=\"{EscapeXml(processName)}\">");

            foreach (var (id, node) in shapes)
            {
                sb.AppendLine($"      <bpmndi:BPMNShape id=\"Shape_{id}\" bpmnElement=\"{id}\">");
                sb.AppendLine($"        <dc:Bounds x=\"{F(node.X)}\" y=\"{F(node.Y)}\" width=\"{F(node.Width)}\" height=\"{F(node.Height)}\" />");
                sb.AppendLine("      </bpmndi:BPMNShape>");
            }

            foreach (var (flowId, line) in edges)
            {
                var start = line?.Start?.Position ?? default;
                var end = line?.End?.Position ?? default;

                sb.AppendLine($"      <bpmndi:BPMNEdge id=\"Edge_{flowId}\" bpmnElement=\"{flowId}\">");
                sb.AppendLine($"        <di:waypoint x=\"{F(start.X)}\" y=\"{F(start.Y)}\" />");
                sb.AppendLine($"        <di:waypoint x=\"{F((start.X + end.X) / 2f)}\" y=\"{F((start.Y + end.Y) / 2f)}\" />");
                sb.AppendLine($"        <di:waypoint x=\"{F(end.X)}\" y=\"{F(end.Y)}\" />");
                sb.AppendLine("      </bpmndi:BPMNEdge>");
            }

            sb.AppendLine("    </bpmndi:BPMNPlane>");
            sb.AppendLine("  </bpmndi:BPMNDiagram>");
        }

        /// <summary>
        /// True when the component's center lies inside the container's rectangle.
        /// Uses explicit geometry because Bounds may be stale before the first render.
        /// </summary>
        private static bool ContainsCenter(SkiaComponent container, SkiaComponent node)
        {
            float cx = node.X + node.Width / 2f;
            float cy = node.Y + node.Height / 2f;
            return cx >= container.X && cx <= container.X + container.Width
                && cy >= container.Y && cy <= container.Y + container.Height;
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Process";
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            return sb.Length == 0 ? "Process" : sb.ToString();
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private string GetBpmnPrefix(BusinessControl node)
        {
            if (node is StartEvent) return "StartEvent";
            if (node is EndEvent) return "EndEvent";
            if (node is IntermediateEventNode) return "IntEvent";
            if (node is Gateway) return "Gateway";
            if (node is BusinessTask) return "Task";
            return "Activity";
        }

        private static string GetTaskElement(string taskType)
        {
            return (taskType ?? "Task").Trim().ToUpperInvariant() switch
            {
                "SERVICE" => "serviceTask",
                "USER" => "userTask",
                "SCRIPT" => "scriptTask",
                "MANUAL" => "manualTask",
                "BUSINESSRULE" => "businessRuleTask",
                "SEND" => "sendTask",
                "RECEIVE" => "receiveTask",
                _ => "task"
            };
        }

        private string GetEventTypeElement(EventType type)
        {
            return type switch
            {
                EventType.Timer => "timerEventDefinition",
                EventType.Message => "messageEventDefinition",
                EventType.Error => "errorEventDefinition",
                EventType.Signal => "signalEventDefinition",
                EventType.Conditional => "conditionalEventDefinition",
                EventType.Escalation => "escalationEventDefinition",
                EventType.Compensation => "compensateEventDefinition",
                EventType.Link => "linkEventDefinition",
                EventType.Terminate => "terminateEventDefinition",
                EventType.Cancel => "cancelEventDefinition",
                _ => "messageEventDefinition"
            };
        }

        private string GetGatewayElement(GatewayType type)
        {
            return type switch
            {
                GatewayType.Parallel => "parallelGateway",
                GatewayType.Inclusive => "inclusiveGateway",
                GatewayType.Complex => "complexGateway",
                GatewayType.EventBased => "eventBasedGateway",
                _ => "exclusiveGateway"
            };
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return System.Net.WebUtility.HtmlEncode(text);
        }
    }
}
