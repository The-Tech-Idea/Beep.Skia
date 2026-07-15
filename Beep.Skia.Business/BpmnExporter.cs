using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Exports a Business Process diagram as BPMN 2.0 XML for interchange with BPMN-compliant tools.
    /// </summary>
    public class BpmnExporter
    {
        public string Export(IReadOnlyList<SkiaComponent> components, IReadOnlyList<Model.IConnectionLine> lines, string processName = "Process")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<bpmn:definitions xmlns:bpmn=\"http://www.omg.org/spec/BPMN/20100524/MODEL\"");
            sb.AppendLine("  xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            sb.AppendLine("  targetNamespace=\"http://bpmn.io/schema/bpmn\">");
            sb.AppendLine($"  <bpmn:process id=\"{EscapeXml(processName)}\" name=\"{EscapeXml(processName)}\">");

            var nodeList = components.Where(c => !c.IsStatic && c is BusinessControl).Cast<BusinessControl>().ToList();
            var idCounter = 0;
            string NextId(string prefix) => $"{prefix}_{++idCounter}";

            foreach (var node in nodeList)
            {
                string id = NextId(GetBpmnPrefix(node));
                node.Tag = id; // Store ID on node for line resolution

                switch (node)
                {
                    case StartEvent _:
                        sb.AppendLine($"    <bpmn:startEvent id=\"{id}\" name=\"{EscapeXml((node).Name)}\" />");
                        break;
                    case EndEvent _:
                        sb.AppendLine($"    <bpmn:endEvent id=\"{id}\" name=\"{EscapeXml((node).Name)}\" />");
                        break;
                    case IntermediateEventNode intEvent:
                        var catchThrow = intEvent.EventPosition == EventPosition.IntermediateThrow ? "intermediateThrowEvent" : "intermediateCatchEvent";
                        sb.AppendLine($"    <bpmn:{catchThrow} id=\"{id}\" name=\"{EscapeXml(intEvent.Label)}\">");
                        sb.AppendLine($"      <bpmn:{GetEventTypeElement(intEvent.EventType)} />");
                        sb.AppendLine($"    </bpmn:{catchThrow}>");
                        break;
                    case Gateway gw:
                        sb.AppendLine($"    <bpmn:{GetGatewayElement(gw.GatewayType)} id=\"{id}\" name=\"{EscapeXml(gw.Name)}\" />");
                        break;
                    case BusinessTask task:
                        sb.AppendLine($"    <bpmn:task id=\"{id}\" name=\"{EscapeXml(task.TaskName)}\" />");
                        break;
                    default:
                        sb.AppendLine($"    <bpmn:task id=\"{id}\" name=\"{EscapeXml((node).Name)}\" />");
                        break;
                }
            }

            // Sequence flows
            foreach (var line in lines)
            {
                var src = line?.Start?.Component as SkiaComponent;
                var dst = line?.End?.Component as SkiaComponent;
                if (src?.Tag is string srcId && dst?.Tag is string dstId)
                {
                    string label = !string.IsNullOrWhiteSpace(line.Label1)
                        ? $" name=\"{EscapeXml(line.Label1)}\""
                        : "";
                    sb.AppendLine($"    <bpmn:sequenceFlow id=\"{NextId("Flow")}\" sourceRef=\"{srcId}\" targetRef=\"{dstId}\"{label} />");
                }
            }

            sb.AppendLine("  </bpmn:process>");
            sb.AppendLine("</bpmn:definitions>");
            return sb.ToString();
        }

        private string GetBpmnPrefix(BusinessControl node)
        {
            if (node is StartEvent) return "StartEvent";
            if (node is EndEvent) return "EndEvent";
            if (node is IntermediateEventNode) return "IntEvent";
            if (node is Gateway) return "Gateway";
            if (node is BusinessTask) return "Task";
            return "Activity";
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
