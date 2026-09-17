using System;
using System.Linq;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.ML;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the ML pipeline JSON exporter: topology, properties, and robustness.
    /// </summary>
    public class MLPipelineExporterTests
    {
        private static DrawingManager BuildPipeline()
        {
            var manager = new DrawingManager();
            var source = new MLDataSourceNode { X = 0, Y = 0, Name = "Source" };
            var preprocess = new MLPreprocessNode { X = 0, Y = 100, Name = "Preprocess" };
            var trainer = new MLTrainerNode { X = 0, Y = 200, Name = "Trainer", Epochs = 25 };
            var evaluate = new MLEvaluateNode { X = 0, Y = 300, Name = "Evaluate" };
            foreach (var c in new SkiaComponent[] { source, preprocess, trainer, evaluate }) manager.AddComponent(c);
            manager.ConnectComponents(source, preprocess, 0, 0);
            manager.ConnectComponents(preprocess, trainer, 0, 0);
            manager.ConnectComponents(trainer, evaluate, 0, 0);
            return manager;
        }

        [Fact]
        public void BuildDefinition_CreatesOrderedNodesAndEdges()
        {
            var manager = BuildPipeline();
            var definition = new MLPipelineExporter().BuildDefinition(manager.GetComponents(), manager.GetLines(), "Demo");

            Assert.Equal("Demo", definition.Name);
            Assert.Equal(4, definition.Nodes.Count);
            Assert.Equal(3, definition.Edges.Count);

            var source = definition.Nodes.Single(n => n.Type == "MLDataSourceNode");
            var preprocess = definition.Nodes.Single(n => n.Type == "MLPreprocessNode");
            var trainer = definition.Nodes.Single(n => n.Type == "MLTrainerNode");
            var evaluate = definition.Nodes.Single(n => n.Type == "MLEvaluateNode");

            Assert.True(source.Order < preprocess.Order);
            Assert.True(preprocess.Order < trainer.Order);
            Assert.True(trainer.Order < evaluate.Order);

            Assert.Equal(25, Convert.ToInt32(trainer.Properties["Epochs"]));
        }

        [Fact]
        public void ExportJson_IsValidJsonWithNodesAndEdges()
        {
            var manager = BuildPipeline();
            var json = new MLPipelineExporter().ExportJson(manager, "Demo");

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal("Demo", root.GetProperty("Name").GetString());
            Assert.Equal("1.0", root.GetProperty("Version").GetString());
            Assert.Equal(4, root.GetProperty("Nodes").GetArrayLength());
            Assert.Equal(3, root.GetProperty("Edges").GetArrayLength());

            var firstNode = root.GetProperty("Nodes")[0];
            Assert.True(firstNode.TryGetProperty("Type", out _));
            Assert.True(firstNode.TryGetProperty("Order", out _));
            Assert.True(firstNode.TryGetProperty("Properties", out _));
        }

        [Fact]
        public void ExportJson_HandlesCyclesWithoutThrowing()
        {
            var manager = new DrawingManager();
            var a = new MLModelNode { Name = "A" };
            var b = new MLModelNode { Name = "B" };
            manager.AddComponent(a);
            manager.AddComponent(b);
            manager.ConnectComponents(a, b, 0, 0);
            manager.ConnectComponents(b, a, 0, 0);

            var json = new MLPipelineExporter().ExportJson(manager.GetComponents(), manager.GetLines());

            using var document = JsonDocument.Parse(json);
            Assert.Equal(2, document.RootElement.GetProperty("Nodes").GetArrayLength());
            Assert.Equal(2, document.RootElement.GetProperty("Edges").GetArrayLength());
        }

        [Fact]
        public void ExportJson_IgnoresNonMlComponents()
        {
            var manager = BuildPipeline();
            manager.AddComponent(new Beep.Skia.UML.UMLClass { ClassName = "NotML" });

            var definition = new MLPipelineExporter().BuildDefinition(manager.GetComponents(), manager.GetLines());

            Assert.Equal(4, definition.Nodes.Count);
            Assert.DoesNotContain(definition.Nodes, n => n.Type.Contains("UML"));
        }
    }
}
