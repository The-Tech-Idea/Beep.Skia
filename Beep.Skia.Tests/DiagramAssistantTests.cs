using System.Linq;
using Beep.Skia;
using Beep.Skia.Assist;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the assisted diagram generation: DSL parsing, layout, DTO building, and provider registry.
    /// </summary>
    public class DiagramAssistantTests
    {
        // ── Flowchart DSL parsing ────────────────────────────────────────────

        [Fact]
        public void Parse_Chain_CreatesNodesAndEdges()
        {
            var graph = FlowchartDslParser.Parse(
                "Start(start) \"Begin\" -> Receive \"Receive order\" -> Valid(decision) \"Valid?\"");

            Assert.Equal(3, graph.Nodes.Count);
            Assert.Equal(2, graph.Edges.Count);

            Assert.Equal("start", graph.Find("Start").TypeKeyword);
            Assert.Equal("Begin", graph.Find("Start").Title);
            Assert.Equal("StartEndNode", graph.Find("Start").ClassName);

            Assert.Equal("Receive order", graph.Find("Receive").Title);
            Assert.Equal("ProcessNode", graph.Find("Receive").ClassName);
            Assert.Equal("DecisionNode", graph.Find("Valid").ClassName);
        }

        [Fact]
        public void Parse_MultipleLines_ReuseNodesAndCarryLabels()
        {
            var graph = FlowchartDslParser.Parse(
                "Valid -> Process \"Process payment\" : yes\n" +
                "Valid -> Notify \"Notify customer\" : no\n" +
                "Process -> End(end) \"End\"\n" +
                "Notify -> End");

            Assert.Equal(4, graph.Nodes.Count);
            Assert.Equal(4, graph.Edges.Count);

            var yes = graph.Edges.Single(e => e.To == "Process");
            Assert.Equal("yes", yes.Label);
            Assert.Equal("no", graph.Edges.Single(e => e.To == "Notify").Label);
            Assert.Null(graph.Edges.Single(e => e.From == "Process").Label);
        }

        [Fact]
        public void Parse_IgnoresCommentsAndBlankLines()
        {
            var graph = FlowchartDslParser.Parse(
                "# a comment\n" +
                "// another\n" +
                "\n" +
                "A -> B");

            Assert.Equal(2, graph.Nodes.Count);
            Assert.Single(graph.Edges);
        }

        [Fact]
        public void Parse_InfersStartAndEndFromNames()
        {
            var graph = FlowchartDslParser.Parse("start -> work -> end");

            Assert.Equal("StartEndNode", graph.Find("start").ClassName);
            Assert.Equal("ProcessNode", graph.Find("work").ClassName);
            Assert.Equal("StartEndNode", graph.Find("end").ClassName);
            Assert.Equal("Start", graph.Find("start").Title);
            Assert.Equal("End", graph.Find("end").Title);
        }

        [Fact]
        public void Parse_UnknownType_WarnsAndFallsBackToProcess()
        {
            var graph = FlowchartDslParser.Parse("X(banana) \"X\"");

            Assert.Equal("ProcessNode", graph.Find("X").ClassName);
            Assert.Contains(graph.Warnings, w => w.Contains("unknown node type"));
        }

        [Fact]
        public void Parse_NodeLimit_TruncatesWithWarning()
        {
            var graph = FlowchartDslParser.Parse("A -> B -> C -> D", maxNodes: 2);

            Assert.Equal(2, graph.Nodes.Count);
            Assert.Contains(graph.Warnings, w => w.Contains("Node limit"));
        }

        // ── Layout ───────────────────────────────────────────────────────────

        [Fact]
        public void Layout_RanksNodesTopDown()
        {
            var graph = FlowchartDslParser.Parse(
                "Start -> Work \"Do work\"\n" +
                "Work -> End");

            Assert.True(graph.Find("Start").Y < graph.Find("Work").Y);
            Assert.True(graph.Find("Work").Y < graph.Find("End").Y);
            Assert.Equal(0, graph.Find("Start").Rank);
            Assert.Equal(2, graph.Find("End").Rank);
        }

        [Fact]
        public void Layout_BranchNodesShareRank()
        {
            var graph = FlowchartDslParser.Parse(
                "Valid -> Yes \"Yes\"\n" +
                "Valid -> No \"No\"");

            Assert.Equal(graph.Find("Yes").Rank, graph.Find("No").Rank);
            Assert.NotEqual(graph.Find("Yes").X, graph.Find("No").X);
        }

        [Fact]
        public void Layout_Cycle_WarnsAndStillLaysOut()
        {
            var graph = FlowchartDslParser.Parse("A -> B -> A");

            Assert.Equal(2, graph.Nodes.Count);
            Assert.Contains(graph.Warnings, w => w.Contains("Cycle"));
            Assert.True(graph.Find("B").Rank > graph.Find("A").Rank);
        }

        // ── Rule-based assistant ─────────────────────────────────────────────

        [Fact]
        public void Generate_Flowchart_BuildsLoadableDto()
        {
            var assistant = new RuleBasedDiagramAssistant();
            var result = assistant.Generate(new DiagramRequest
            {
                Prompt = "Start(start) \"Start\" -> Valid(decision) \"Valid?\" -> End(end) \"End\"\n" +
                         "Valid -> Retry \"Retry\" : no\n" +
                         "Retry -> Valid"
            });

            Assert.True(result.Success);
            Assert.Equal("rule-based", result.Provider);
            Assert.Equal(4, result.Diagram.Components.Count);
            Assert.Equal(4, result.Diagram.Lines.Count);
            Assert.Contains("4 node", result.Explanation);

            var start = result.Diagram.Components[0];
            Assert.Equal("Beep.Skia.Flowchart.StartEndNode, Beep.Skia.FlowChart", start.Type);
            Assert.Equal("Start", start.PropertyBag["Title"]);

            var labeled = result.Diagram.Lines.Single(l => l.Label1 == "no");
            Assert.Equal(1, labeled.StartComponentIndex);
            Assert.Equal(3, labeled.EndComponentIndex);
        }

        [Fact]
        public void Generate_Flowchart_LoadsIntoDrawingManager()
        {
            var assistant = new RuleBasedDiagramAssistant();
            var result = assistant.Generate(new DiagramRequest
            {
                Prompt = "Start -> Work \"Do work\" -> End"
            });
            Assert.True(result.Success);

            var manager = new DrawingManager();
            manager.LoadFromDto(result.Diagram);

            Assert.Equal(3, manager.GetComponents().Count);
            Assert.Equal(2, manager.GetLines().Count);
        }

        [Fact]
        public void Generate_EmptyPrompt_Fails()
        {
            var result = new RuleBasedDiagramAssistant().Generate(new DiagramRequest { Prompt = "   " });

            Assert.False(result.Success);
            Assert.NotEmpty(result.Warnings);
        }

        [Fact]
        public void Generate_MindMap_BuildsRadialTree()
        {
            var result = new RuleBasedDiagramAssistant().Generate(new DiagramRequest
            {
                DiagramKind = "mindmap",
                Prompt = "Product\n  Users\n    Personas\n  Pricing"
            });

            Assert.True(result.Success);
            Assert.Equal(4, result.Diagram.Components.Count);
            Assert.Equal(3, result.Diagram.Lines.Count);

            var root = result.Diagram.Components[0];
            Assert.Equal("Beep.Skia.MindMap.CentralNode, Beep.Skia.MindMap", root.Type);
            Assert.Equal("Product", root.Name);
            Assert.True(root.X < 420 && root.X > 300, "Root should sit near the layout center");

            Assert.Equal("Beep.Skia.MindMap.TopicNode, Beep.Skia.MindMap", result.Diagram.Components[1].Type);
            Assert.Equal("Beep.Skia.MindMap.SubTopicNode, Beep.Skia.MindMap", result.Diagram.Components[2].Type);
        }

        [Fact]
        public void Generate_AutoDetectsMindMapFromIndentation()
        {
            var result = new RuleBasedDiagramAssistant().Generate(new DiagramRequest
            {
                Prompt = "Root\n  Child A\n  Child B"
            });

            Assert.True(result.Success);
            Assert.Equal("Beep.Skia.MindMap.CentralNode, Beep.Skia.MindMap", result.Diagram.Components[0].Type);
        }

        // ── Registry ─────────────────────────────────────────────────────────

        private sealed class FailingAssistant : IDiagramAssistant
        {
            public string Name => "failing";
            public DiagramSuggestion Generate(DiagramRequest request)
                => DiagramSuggestion.Failed(Name, "provider offline");
        }

        private sealed class StubAssistant : IDiagramAssistant
        {
            public string Name => "stub";
            public DiagramSuggestion Generate(DiagramRequest request)
                => new DiagramSuggestion { Success = true, Provider = Name, Diagram = new Beep.Skia.Serialization.DiagramDto() };
        }

        [Fact]
        public void Registry_FallsBackToRuleBasedWhenProviderFails()
        {
            var registry = new DiagramAssistantRegistry();
            registry.Register(new FailingAssistant(), priority: 0);

            var result = registry.Generate(new DiagramRequest { Prompt = "A -> B" });

            Assert.True(result.Success);
            Assert.Equal("rule-based", result.Provider);
        }

        [Fact]
        public void Registry_PrefersHigherPriorityAssistant()
        {
            var registry = new DiagramAssistantRegistry();
            registry.Register(new StubAssistant(), priority: 0);

            var result = registry.Generate(new DiagramRequest { Prompt = "A -> B" });

            Assert.True(result.Success);
            Assert.Equal("stub", result.Provider);
        }

        [Fact]
        public void Registry_AllFail_ReportsAttempts()
        {
            var registry = new DiagramAssistantRegistry(includeDefaultAssistant: false);
            registry.Register(new FailingAssistant());

            var result = registry.Generate(new DiagramRequest { Prompt = "A -> B" });

            Assert.False(result.Success);
            Assert.Contains(result.Warnings, w => w.Contains("failing"));
        }

        [Fact]
        public void Registry_Unregister_RemovesAssistant()
        {
            var registry = new DiagramAssistantRegistry();
            Assert.True(registry.Unregister("rule-based"));
            Assert.Empty(registry.Assistants);
        }
    }
}
