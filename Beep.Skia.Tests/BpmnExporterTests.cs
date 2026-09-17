using System.Linq;
using Beep.Skia;
using Beep.Skia.Business;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for BPMN 2.0 export: semantic elements, task types, sequence flows, and BPMN DI.
    /// </summary>
    public class BpmnExporterTests
    {
        private static DrawingManager BuildProcess(out StartEvent start, out BusinessTask task, out EndEvent end)
        {
            var manager = new DrawingManager();
            start = new StartEvent { X = 100, Y = 60, Width = 40, Height = 40, Name = "Start" };
            task = new BusinessTask { X = 220, Y = 50, Width = 120, Height = 60, Label = "Approve" };
            end = new EndEvent { X = 400, Y = 60, Width = 40, Height = 40, Name = "End" };
            manager.AddComponent(start);
            manager.AddComponent(task);
            manager.AddComponent(end);
            manager.ConnectComponents(start, task, 0, 0);
            manager.ConnectComponents(task, end, 0, 0);
            return manager;
        }

        [Fact]
        public void Export_EmitsSemanticElementsAndFlows()
        {
            var manager = BuildProcess(out _, out _, out _);
            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines(), "Approval");

            Assert.Contains("<bpmn:process id=\"Approval\"", xml);
            Assert.Contains("<bpmn:startEvent", xml);
            Assert.Contains("<bpmn:endEvent", xml);
            Assert.Contains("<bpmn:task", xml);
            Assert.Contains("name=\"Approve\"", xml);
            Assert.Equal(2, CountOccurrences(xml, "<bpmn:sequenceFlow"));
        }

        [Fact]
        public void Export_MapsTaskTypes()
        {
            var manager = new DrawingManager();
            var service = new BusinessTask { Label = "Call API", TaskType = "Service" };
            var user = new BusinessTask { Label = "Review", TaskType = "User" };
            var script = new BusinessTask { Label = "Transform", TaskType = "Script" };
            var plain = new BusinessTask { Label = "Plain" };
            manager.AddComponent(service);
            manager.AddComponent(user);
            manager.AddComponent(script);
            manager.AddComponent(plain);

            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("<bpmn:serviceTask", xml);
            Assert.Contains("<bpmn:userTask", xml);
            Assert.Contains("<bpmn:scriptTask", xml);
            Assert.Contains("name=\"Plain\"", xml);
        }

        [Fact]
        public void Export_EmitsDiagramInterchangeWithBoundsAndWaypoints()
        {
            var manager = BuildProcess(out var start, out var task, out _);
            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines(), "Approval");

            Assert.Contains("<bpmndi:BPMNDiagram", xml);
            Assert.Contains("<bpmndi:BPMNPlane", xml);
            Assert.Contains("<bpmndi:BPMNShape", xml);
            Assert.Contains("<dc:Bounds", xml);
            Assert.Contains($"x=\"{start.X}\"", xml);
            Assert.Contains($"width=\"{task.Width}\"", xml);
            Assert.Contains("<bpmndi:BPMNEdge", xml);
            Assert.Contains("<di:waypoint", xml);
        }

        [Fact]
        public void Export_MapsGatewaysAndEvents()
        {
            var manager = new DrawingManager();
            var gateway = new Gateway { Name = "Split", GatewayType = GatewayType.Parallel };
            var intEvent = new IntermediateEventNode { Label = "Wait", EventType = EventType.Timer };
            manager.AddComponent(gateway);
            manager.AddComponent(intEvent);

            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("<bpmn:parallelGateway", xml);
            Assert.Contains("<bpmn:intermediateCatchEvent", xml);
            Assert.Contains("<bpmn:timerEventDefinition", xml);
        }

        [Fact]
        public void Export_EscapesXmlCharacters()
        {
            var manager = new DrawingManager();
            var task = new BusinessTask { Label = "R&D <review>" };
            manager.AddComponent(task);

            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines());

            Assert.Contains("R&amp;D &lt;review&gt;", xml);
        }

        private static int CountOccurrences(string text, string token)
            => text.Split(new[] { token }, System.StringSplitOptions.None).Length - 1;

        [Fact]
        public void Pools_EmitCollaborationWithParticipantsAndProcesses()
        {
            var manager = new DrawingManager();
            var poolA = new BpmnPoolNode { X = 0, Y = 0, Width = 420, Height = 220, PoolName = "Customer" };
            var poolB = new BpmnPoolNode { X = 500, Y = 0, Width = 420, Height = 220, PoolName = "Supplier" };
            var taskA = new BusinessTask { X = 60, Y = 80, Width = 120, Height = 60, Label = "Order" };
            var taskB = new BusinessTask { X = 560, Y = 80, Width = 120, Height = 60, Label = "Ship" };
            manager.AddComponent(poolA);
            manager.AddComponent(poolB);
            manager.AddComponent(taskA);
            manager.AddComponent(taskB);

            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines(), "Process");

            Assert.Contains("<bpmn:collaboration", xml);
            Assert.Equal(2, CountOccurrences(xml, "<bpmn:participant"));
            Assert.Contains("name=\"Customer\"", xml);
            Assert.Contains("name=\"Supplier\"", xml);
            Assert.Equal(2, CountOccurrences(xml, "<bpmn:process"));
            Assert.Contains("name=\"Order\"", xml);
            Assert.Contains("name=\"Ship\"", xml);
        }

        [Fact]
        public void CrossPoolLine_BecomesMessageFlow()
        {
            var manager = new DrawingManager();
            var poolA = new BpmnPoolNode { X = 0, Y = 0, Width = 420, Height = 220, PoolName = "Customer" };
            var poolB = new BpmnPoolNode { X = 500, Y = 0, Width = 420, Height = 220, PoolName = "Supplier" };
            var taskA = new BusinessTask { X = 60, Y = 80, Width = 120, Height = 60, Label = "Order" };
            var taskB = new BusinessTask { X = 560, Y = 80, Width = 120, Height = 60, Label = "Ship" };
            foreach (var c in new SkiaComponent[] { poolA, poolB, taskA, taskB }) manager.AddComponent(c);
            manager.ConnectComponents(taskA, taskB, 0, 0);

            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines(), "Process");

            Assert.Contains("<bpmn:messageFlow", xml);
            Assert.DoesNotContain("<bpmn:sequenceFlow", xml);
        }

        [Fact]
        public void ExplicitBpmnMessageFlow_WithinSamePool_IsMessageFlow()
        {
            var manager = new DrawingManager();
            var pool = new BpmnPoolNode { X = 0, Y = 0, Width = 500, Height = 240, PoolName = "Ops" };
            var t1 = new BusinessTask { X = 60, Y = 80, Width = 120, Height = 60, Label = "A" };
            var t2 = new BusinessTask { X = 280, Y = 80, Width = 120, Height = 60, Label = "B" };
            manager.AddComponent(pool);
            manager.AddComponent(t1);
            manager.AddComponent(t2);

            var messageFlow = new BpmnMessageFlow
            {
                Start = t1.OutConnectionPoints[0],
                End = t2.InConnectionPoints[0]
            };

            var xml = new BpmnExporter().Export(
                manager.GetComponents(),
                new[] { (Beep.Skia.Model.IConnectionLine)messageFlow },
                "Process");

            Assert.Contains("<bpmn:messageFlow", xml);
            Assert.DoesNotContain("<bpmn:sequenceFlow", xml);
        }

        [Fact]
        public void Lanes_EmitLaneSetWithFlowNodeRefs()
        {
            var manager = new DrawingManager();
            var pool = new BpmnPoolNode { X = 0, Y = 0, Width = 500, Height = 300, PoolName = "Ops" };
            var lane = new BpmnLaneNode { X = 20, Y = 40, Width = 460, Height = 110, LaneName = "Sales" };
            var task = new BusinessTask { X = 80, Y = 80, Width = 120, Height = 60, Label = "Quote" };
            manager.AddComponent(pool);
            manager.AddComponent(lane);
            manager.AddComponent(task);

            var xml = new BpmnExporter().Export(manager.GetComponents(), manager.GetLines(), "Process");

            Assert.Contains("<bpmn:laneSet", xml);
            Assert.Contains("<bpmn:lane ", xml);
            Assert.Contains("name=\"Sales\"", xml);

            var taskId = task.Tag as string;
            Assert.False(string.IsNullOrEmpty(taskId));
            Assert.Contains($"<bpmn:flowNodeRef>{taskId}</bpmn:flowNodeRef>", xml);
        }
    }
}
