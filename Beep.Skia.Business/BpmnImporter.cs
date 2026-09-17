using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Beep.Skia.Business
{
    /// <summary>
    /// A flow parsed from BPMN XML, ready to be connected in a drawing manager.
    /// </summary>
    public class BpmnFlow
    {
        public SkiaComponent Source { get; set; }
        public SkiaComponent Target { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// Result of parsing BPMN XML.
    /// </summary>
    public class BpmnImportResult
    {
        public string ProcessName { get; set; }
        public List<SkiaComponent> Components { get; } = new List<SkiaComponent>();
        public List<BpmnFlow> Flows { get; } = new List<BpmnFlow>();
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Adds the parsed components to a drawing manager and creates the flows.
        /// </summary>
        public void LoadInto(DrawingManager manager)
        {
            if (manager == null) return;

            foreach (var component in Components)
            {
                manager.AddComponent(component);
            }

            foreach (var flow in Flows)
            {
                if (flow.Source == null || flow.Target == null) continue;
                if (manager.ConnectComponents(flow.Source, flow.Target, 0, 0))
                {
                    var line = manager.GetLines().LastOrDefault();
                    if (line != null && !string.IsNullOrWhiteSpace(flow.Label))
                        line.Label1 = flow.Label;
                }
            }
        }
    }

    /// <summary>
    /// Imports BPMN 2.0 XML into Business diagram components.
    /// Namespace-agnostic (matches by local element name) and reads BPMN DI bounds when present.
    /// </summary>
    public class BpmnImporter
    {
        /// <summary>
        /// Parses BPMN XML into components and flows.
        /// </summary>
        public BpmnImportResult Parse(string xml)
        {
            var result = new BpmnImportResult();
            if (string.IsNullOrWhiteSpace(xml)) return result;

            XDocument document;
            try
            {
                document = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                result.Warnings.Add("Invalid XML: " + ex.Message);
                return result;
            }

            var process = document.Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "process", StringComparison.OrdinalIgnoreCase));
            if (process == null)
            {
                result.Warnings.Add("No bpmn:process element found.");
                return result;
            }

            result.ProcessName = process.Attribute("name")?.Value ?? process.Attribute("id")?.Value;

            var shapeBounds = ReadShapeBounds(document);
            var byId = new Dictionary<string, SkiaComponent>(StringComparer.Ordinal);

            foreach (var element in process.Elements())
            {
                var component = CreateComponent(element, result.Warnings);
                if (component == null) continue;

                var id = element.Attribute("id")?.Value;
                if (!string.IsNullOrWhiteSpace(id)) byId[id] = component;

                if (id != null && shapeBounds.TryGetValue(id, out var bounds))
                {
                    component.X = bounds.X;
                    component.Y = bounds.Y;
                    if (bounds.Width > 0) component.Width = bounds.Width;
                    if (bounds.Height > 0) component.Height = bounds.Height;
                }

                result.Components.Add(component);
            }

            foreach (var flow in process.Elements().Where(e => string.Equals(e.Name.LocalName, "sequenceFlow", StringComparison.OrdinalIgnoreCase)))
            {
                var sourceRef = flow.Attribute("sourceRef")?.Value;
                var targetRef = flow.Attribute("targetRef")?.Value;
                if (sourceRef == null || targetRef == null) continue;
                if (!byId.TryGetValue(sourceRef, out var source) || !byId.TryGetValue(targetRef, out var target))
                {
                    result.Warnings.Add($"Flow references unknown element(s): {sourceRef} -> {targetRef}");
                    continue;
                }

                result.Flows.Add(new BpmnFlow
                {
                    Source = source,
                    Target = target,
                    Label = flow.Attribute("name")?.Value
                });
            }

            return result;
        }

        /// <summary>
        /// Parses and loads BPMN XML directly into a drawing manager.
        /// </summary>
        public BpmnImportResult LoadInto(DrawingManager manager, string xml)
        {
            var result = Parse(xml);
            result.LoadInto(manager);
            return result;
        }

        private static SkiaComponent CreateComponent(XElement element, List<string> warnings)
        {
            var localName = element.Name.LocalName;
            var name = element.Attribute("name")?.Value ?? string.Empty;

            switch (localName)
            {
                case "startEvent":
                    return new StartEvent { Name = string.IsNullOrWhiteSpace(name) ? "Start" : name };

                case "endEvent":
                    return new EndEvent { Name = string.IsNullOrWhiteSpace(name) ? "End" : name };

                case "task":
                case "serviceTask":
                case "userTask":
                case "scriptTask":
                case "manualTask":
                case "businessRuleTask":
                case "sendTask":
                case "receiveTask":
                    return new BusinessTask
                    {
                        Label = string.IsNullOrWhiteSpace(name) ? "Task" : name,
                        TaskType = MapTaskType(localName)
                    };

                case "exclusiveGateway":
                case "parallelGateway":
                case "inclusiveGateway":
                case "complexGateway":
                case "eventBasedGateway":
                    return new Gateway
                    {
                        Name = string.IsNullOrWhiteSpace(name) ? "Gateway" : name,
                        GatewayType = MapGatewayType(localName)
                    };

                case "intermediateCatchEvent":
                case "intermediateThrowEvent":
                    return new IntermediateEventNode
                    {
                        Label = name,
                        EventPosition = localName == "intermediateThrowEvent" ? EventPosition.IntermediateThrow : EventPosition.IntermediateCatch,
                        EventType = ReadEventType(element)
                    };

                case "subProcess":
                    return new SubProcess { Name = string.IsNullOrWhiteSpace(name) ? "SubProcess" : name };

                default:
                    // Ignore unsupported elements (data objects, lanes, etc.) silently.
                    return null;
            }
        }

        private static EventType ReadEventType(XElement element)
        {
            var definition = element.Elements().FirstOrDefault();
            var definitionName = definition?.Name.LocalName ?? string.Empty;
            return definitionName switch
            {
                "timerEventDefinition" => EventType.Timer,
                "messageEventDefinition" => EventType.Message,
                "errorEventDefinition" => EventType.Error,
                "signalEventDefinition" => EventType.Signal,
                "conditionalEventDefinition" => EventType.Conditional,
                "escalationEventDefinition" => EventType.Escalation,
                "compensateEventDefinition" => EventType.Compensation,
                "linkEventDefinition" => EventType.Link,
                "terminateEventDefinition" => EventType.Terminate,
                "cancelEventDefinition" => EventType.Cancel,
                _ => EventType.Message
            };
        }

        private static string MapTaskType(string elementName) => elementName switch
        {
            "serviceTask" => "Service",
            "userTask" => "User",
            "scriptTask" => "Script",
            "manualTask" => "Manual",
            "businessRuleTask" => "BusinessRule",
            "sendTask" => "Send",
            "receiveTask" => "Receive",
            _ => "Task"
        };

        private static GatewayType MapGatewayType(string elementName) => elementName switch
        {
            "parallelGateway" => GatewayType.Parallel,
            "inclusiveGateway" => GatewayType.Inclusive,
            "complexGateway" => GatewayType.Complex,
            "eventBasedGateway" => GatewayType.EventBased,
            _ => GatewayType.Exclusive
        };

        private static Dictionary<string, (float X, float Y, float Width, float Height)> ReadShapeBounds(XDocument document)
        {
            var bounds = new Dictionary<string, (float, float, float, float)>(StringComparer.Ordinal);

            foreach (var shape in document.Descendants()
                         .Where(e => string.Equals(e.Name.LocalName, "BPMNShape", StringComparison.OrdinalIgnoreCase)))
            {
                var elementId = shape.Attribute("bpmnElement")?.Value;
                if (string.IsNullOrWhiteSpace(elementId)) continue;

                var boundsElement = shape.Elements()
                    .FirstOrDefault(e => string.Equals(e.Name.LocalName, "Bounds", StringComparison.OrdinalIgnoreCase));
                if (boundsElement == null) continue;

                bounds[elementId] = (
                    ParseFloat(boundsElement.Attribute("x")?.Value),
                    ParseFloat(boundsElement.Attribute("y")?.Value),
                    ParseFloat(boundsElement.Attribute("width")?.Value),
                    ParseFloat(boundsElement.Attribute("height")?.Value));
            }

            return bounds;
        }

        private static float ParseFloat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0f;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0f;
        }
    }
}
