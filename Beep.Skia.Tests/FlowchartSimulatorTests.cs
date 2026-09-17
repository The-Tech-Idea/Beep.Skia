using System.Linq;
using Beep.Skia;
using Beep.Skia.Flowchart;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="FlowchartSimulator"/> step-through execution.
    /// </summary>
    public class FlowchartSimulatorTests
    {
        [Fact]
        public void LinearFlow_StepsThroughAndFinishes()
        {
            var manager = new DrawingManager();
            var start = new StartEndNode { X = 0, Y = 0, Label = "Start" };
            var process = new ProcessNode { X = 0, Y = 100, Label = "work" };
            var end = new StartEndNode { X = 0, Y = 200, Label = "End" };
            foreach (var c in new SkiaComponent[] { start, process, end }) manager.AddComponent(c);
            manager.ConnectComponents(start, process, 0, 0);
            manager.ConnectComponents(process, end, 0, 0);

            var sim = new FlowchartSimulator();
            sim.Reset(manager.GetComponents(), manager.GetLines());

            Assert.Equal(start, sim.CurrentNode);
            Assert.False(sim.IsFinished);

            sim.Step();
            Assert.Equal(process, sim.CurrentNode);

            sim.Step();
            Assert.Equal(end, sim.CurrentNode);

            sim.Step();
            Assert.True(sim.IsFinished);
            Assert.Equal(3, sim.StepCount);
            Assert.Equal(new[] { "Start", "work", "End" }, sim.Trace);
        }

        [Fact]
        public void Decision_BranchSelectorChoosesFalsePath()
        {
            var manager = new DrawingManager();
            var start = new StartEndNode { X = 0, Y = 0, Label = "Start" };
            var decision = new DecisionNode { X = 0, Y = 100, Label = "x > 0" };
            var positive = new ProcessNode { X = -120, Y = 200, Label = "positive" };
            var negative = new ProcessNode { X = 120, Y = 200, Label = "negative" };
            var end = new StartEndNode { X = 0, Y = 300, Label = "End" };
            foreach (var c in new SkiaComponent[] { start, decision, positive, negative, end }) manager.AddComponent(c);

            manager.ConnectComponents(start, decision, 0, 0);
            manager.ConnectComponents(decision, positive, 0, 0);
            manager.ConnectComponents(decision, negative, 1, 0);
            manager.ConnectComponents(positive, end, 0, 0);
            manager.ConnectComponents(negative, end, 0, 0);

            var sim = new FlowchartSimulator
            {
                // Always take the false branch (port 1).
                BranchSelector = (node, edges) => edges.FirstOrDefault(e => e.PortIndex == 1) ?? edges[0]
            };
            sim.Reset(manager.GetComponents(), manager.GetLines());
            var finished = sim.Run();

            Assert.True(finished);
            Assert.Contains("negative", sim.Trace);
            Assert.DoesNotContain("positive", sim.Trace);
        }

        [Fact]
        public void Cycle_IsCappedByRunLimit()
        {
            var manager = new DrawingManager();
            var a = new ProcessNode { X = 0, Y = 0, Label = "A" };
            var b = new ProcessNode { X = 0, Y = 100, Label = "B" };
            manager.AddComponent(a);
            manager.AddComponent(b);
            manager.ConnectComponents(a, b, 0, 0);
            manager.ConnectComponents(b, a, 0, 0);

            var sim = new FlowchartSimulator();
            sim.Reset(manager.GetComponents(), manager.GetLines());
            var finished = sim.Run(maxSteps: 10);

            Assert.False(finished);
            Assert.Equal(10, sim.StepCount);
        }

        [Fact]
        public void SteppedEvent_FiresForEachStep()
        {
            var manager = new DrawingManager();
            var start = new StartEndNode { X = 0, Y = 0, Label = "Start" };
            var end = new StartEndNode { X = 0, Y = 100, Label = "End" };
            manager.AddComponent(start);
            manager.AddComponent(end);
            manager.ConnectComponents(start, end, 0, 0);

            var sim = new FlowchartSimulator();
            int steps = 0;
            sim.Stepped += (s, e) => steps++;
            sim.Reset(manager.GetComponents(), manager.GetLines());
            sim.Run();

            Assert.Equal(2, steps);
        }
    }
}
