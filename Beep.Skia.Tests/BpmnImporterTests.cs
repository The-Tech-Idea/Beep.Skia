using System.Linq;
using Beep.Skia;
using Beep.Skia.Business;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="BpmnImporter"/>: export/import round-trip, task/gateway/event
    /// mapping, DI bounds, and warning handling.
    /// </summary>
    public class BpmnImporterTests
    {
        private static DrawingManager BuildProcess()
        {
            var manager = new DrawingManager();
            var start = new StartEvent { X = 10, Y = 20, Width = 40, Height = 40, Name = "Start" };
            var task = new BusinessTask { X = 100, Y = 10, Width = 120, Height = 60, Label = "Approve", TaskType = "User" };
            var gateway = new Gateway { X = 260, Y = 10, Width = 50, Height = 50, Name = "Split", GatewayType = GatewayType.Parallel };
            var intEvent = new IntermediateEventNode { X = 350, Y = 10, Width = 50, Height = 50, Label = "Wait", EventType = EventType.Timer };
            var end = new EndEvent { X = 450, Y = 20, Width = 40, Height = 40, Name = "End" };
            foreach (var c in new SkiaComponent[] { start, task, gateway, intEvent, end }) manager.AddComponent(c);

            manager.ConnectComponents(start, task, 0, 0);
            manager.ConnectComponents(task, gateway, 0, 0);
            manager.ConnectComponents(gateway, intEvent, 0, 0);
            manager.ConnectComponents(intEvent, end, 0, 0);
            return manager;
        }

        [Fact]
        public void RoundTrip_PreservesComponentsAndFlows()
        {
            var manager = BuildProcess();
            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines(), "Approval");

            var imported = new BpmnImporter().Parse(xml);

            Assert.Empty(imported.Warnings);
            Assert.Equal(5, imported.Components.Count);
            Assert.Single(imported.Components.OfType<StartEvent>());
            Assert.Single(imported.Components.OfType<EndEvent>());
            Assert.Single(imported.Components.OfType<BusinessTask>());
            Assert.Single(imported.Components.OfType<Gateway>());
            Assert.Single(imported.Components.OfType<IntermediateEventNode>());
            Assert.Equal(4, imported.Flows.Count);
        }

        [Fact]
        public void RoundTrip_PreservesTaskGatewayAndEventTypes()
        {
            var manager = BuildProcess();
            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines());

            var imported = new BpmnImporter().Parse(xml);

            var task = imported.Components.OfType<BusinessTask>().Single();
            Assert.Equal("Approve", task.Label);
            Assert.Equal("User", task.TaskType);

            var gateway = imported.Components.OfType<Gateway>().Single();
            Assert.Equal(GatewayType.Parallel, gateway.GatewayType);

            var intEvent = imported.Components.OfType<IntermediateEventNode>().Single();
            Assert.Equal(EventType.Timer, intEvent.EventType);
            Assert.Equal(EventPosition.IntermediateCatch, intEvent.EventPosition);
        }

        [Fact]
        public void RoundTrip_AppliesDiagramInterchangeBounds()
        {
            var manager = BuildProcess();
            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines());

            var imported = new BpmnImporter().Parse(xml);

            var start = imported.Components.OfType<StartEvent>().Single();
            Assert.Equal(10f, start.X);
            Assert.Equal(20f, start.Y);
            Assert.Equal(40f, start.Width);
            Assert.Equal(40f, start.Height);

            var task = imported.Components.OfType<BusinessTask>().Single();
            Assert.Equal(100f, task.X);
            Assert.Equal(120f, task.Width);
        }

        [Fact]
        public void LoadInto_CreatesComponentsAndConnections()
        {
            var source = BuildProcess();
            var xml = new BpmnExporter().Export(source.GetComponents(), source.GetLines());

            var target = new DrawingManager();
            var result = new BpmnImporter().LoadInto(target, xml);

            Assert.Empty(result.Warnings);
            Assert.Equal(5, target.GetComponents().Count);
            Assert.Equal(4, target.GetLines().Count);
        }

        [Fact]
        public void Parse_ReportsUnknownFlowReferences()
        {
            const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL">
              <bpmn:process id="p1">
                <bpmn:startEvent id="start_1" name="Start" />
                <bpmn:sequenceFlow id="flow_1" sourceRef="start_1" targetRef="missing_1" />
              </bpmn:process>
            </bpmn:definitions>
            """;

            var imported = new BpmnImporter().Parse(xml);

            Assert.Single(imported.Components);
            Assert.Empty(imported.Flows);
            Assert.Contains(imported.Warnings, w => w.Contains("unknown element"));
        }

        [Fact]
        public void Parse_InvalidXml_ReportsWarning()
        {
            var imported = new BpmnImporter().Parse("<not-xml");
            Assert.NotEmpty(imported.Warnings);
        }
    }
}
