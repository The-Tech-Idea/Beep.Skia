using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Serialization;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Regression tests for the automation save/load path: connections and node configuration must
    /// survive a diagram round-trip, otherwise a saved automation workflow silently loses its wiring.
    /// </summary>
    public class AutomationSerializationTests
    {
        private static (DrawingManager Manager, ManualTriggerNode Start, DataTransformNode Transform) Build()
        {
            var manager = new DrawingManager();
            var start = new ManualTriggerNode { X = 60, Y = 40, Width = 160, Height = 60, Name = "Start" };
            var transform = new DataTransformNode { X = 60, Y = 180, Width = 160, Height = 60, Name = "Transform" };
            transform.FieldMappings = new List<FieldMapping>
            {
                new FieldMapping { SourceField = "amount", TargetField = "total" }
            };

            manager.AddComponent(start);
            manager.AddComponent(transform);
            manager.ConnectComponents(start, transform);
            return (manager, start, transform);
        }

        private static DrawingManager RoundTrip(DrawingManager manager)
        {
            var json = JsonSerializer.Serialize(manager.ToDto());
            var reloaded = new DrawingManager();
            reloaded.LoadFromDto(JsonSerializer.Deserialize<DiagramDto>(json));
            return reloaded;
        }

        [Fact]
        public void AutomationNodes_ExposeTheirPortsThroughTheCanvasCollections()
        {
            var (_, start, transform) = Build();

            // The ports must live where the drawing manager and serialization look for them.
            Assert.Single(start.OutConnectionPoints);
            Assert.Single(transform.InConnectionPoints);
            Assert.Empty(start.InConnectionPoints);       // a trigger has no input
            Assert.Single(transform.OutConnectionPoints);
        }

        [Fact]
        public void RoundTrip_PreservesAutomationConnections()
        {
            var (manager, _, _) = Build();
            Assert.Single(manager.GetLines());

            var reloaded = RoundTrip(manager);

            Assert.Equal(2, reloaded.GetComponents().Count);
            var line = Assert.Single(reloaded.GetLines());
            Assert.Equal("Start", line.Start?.Component?.Name);
            Assert.Equal("Transform", line.End?.Component?.Name);
        }

        [Fact]
        public void RoundTrip_PreservesAutomationConfiguration()
        {
            var (manager, _, _) = Build();

            var reloaded = RoundTrip(manager);
            var transform = reloaded.GetComponents().OfType<DataTransformNode>().Single();

            // The runtime configuration is what survives the round-trip; strongly typed properties
            // (FieldMappings, Filters) are hydrated from it when the node initializes for execution.
            Assert.True(transform.Configuration.TryGetValue("FieldMappings", out var mappings),
                "the node configuration must survive the round-trip");

            var entries = Assert.IsAssignableFrom<System.Collections.IEnumerable>(mappings);
            var first = entries.Cast<object>().First() as IDictionary<string, object>;
            Assert.NotNull(first);
            Assert.Equal("amount", first["SourceField"]?.ToString());
            Assert.Equal("total", first["TargetField"]?.ToString());
        }

        [Fact]
        public void RoundTrip_ProducesAnExecutableWorkflow()
        {
            var (manager, _, _) = Build();

            var reloaded = RoundTrip(manager);
            var workflow = reloaded.ToWorkflowDefinition("Round Tripped");

            Assert.Equal(2, workflow.Nodes.Count);
            Assert.Single(workflow.Connections);
        }
    }
}
