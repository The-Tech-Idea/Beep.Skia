using System.Linq;
using Beep.Skia;
using Beep.Skia.Flowchart;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="FlowchartCodeGenerator"/>.
    /// Conventions: decision port 0 = true branch, port 1 = false branch;
    /// loop port 0 = body, port 1 = exit.
    /// </summary>
    public class FlowchartCodeGeneratorTests
    {
        [Fact]
        public void LinearFlow_GeneratesStatementsInOrder()
        {
            var manager = new DrawingManager();
            var start = new StartEndNode { X = 0, Y = 0, Label = "Start" };
            var process = new ProcessNode { X = 0, Y = 100, Label = "Do work" };
            var end = new StartEndNode { X = 0, Y = 200, Label = "End" };
            manager.AddComponent(start);
            manager.AddComponent(process);
            manager.AddComponent(end);
            manager.ConnectComponents(start, process, 0, 0);
            manager.ConnectComponents(process, end, 0, 0);

            var code = new FlowchartCodeGenerator().Generate(manager.GetComponents(), manager.GetLines(), CodeLanguage.Pseudocode);

            Assert.Contains("BEGIN", code);
            Assert.Contains("Do work;", code);
            Assert.Contains("END", code);
            Assert.True(code.IndexOf("Do work;", System.StringComparison.Ordinal) < code.LastIndexOf("END", System.StringComparison.Ordinal));
        }

        [Fact]
        public void Decision_GeneratesIfElseForBothBranches()
        {
            var manager = new DrawingManager();
            var start = new StartEndNode { X = 0, Y = 0, Label = "Start" };
            var decision = new DecisionNode { X = 0, Y = 100, Label = "x > 0" };
            var positive = new ProcessNode { X = -120, Y = 200, Label = "positive" };
            var negative = new ProcessNode { X = 120, Y = 200, Label = "negative" };
            var end = new StartEndNode { X = 0, Y = 300, Label = "End" };
            foreach (var c in new SkiaComponent[] { start, decision, positive, negative, end }) manager.AddComponent(c);

            manager.ConnectComponents(start, decision, 0, 0);
            manager.ConnectComponents(decision, positive, 0, 0); // true
            manager.ConnectComponents(decision, negative, 1, 0); // false
            manager.ConnectComponents(positive, end, 0, 0);
            manager.ConnectComponents(negative, end, 0, 0);

            var python = new FlowchartCodeGenerator().Generate(manager.GetComponents(), manager.GetLines(), CodeLanguage.Python);

            Assert.Contains("if x > 0:", python);
            Assert.Contains("positive", python);
            Assert.Contains("negative", python);
            Assert.Contains("else:", python);

            var csharp = new FlowchartCodeGenerator().Generate(manager.GetComponents(), manager.GetLines(), CodeLanguage.CSharp);
            Assert.Contains("if (x > 0)", csharp);
            Assert.Contains("return;", csharp);
        }

        [Fact]
        public void WhileLoop_GeneratesWhileConstructWithBody()
        {
            var manager = new DrawingManager();
            var start = new StartEndNode { X = 0, Y = 0, Label = "Start" };
            var loop = new WhileLoopNode { X = 0, Y = 100, Condition = "i < 10" };
            var body = new ProcessNode { X = 0, Y = 200, Label = "work" };
            var end = new StartEndNode { X = 0, Y = 300, Label = "End" };
            foreach (var c in new SkiaComponent[] { start, loop, body, end }) manager.AddComponent(c);

            manager.ConnectComponents(start, loop, 0, 0);
            manager.ConnectComponents(loop, body, 0, 0);   // body
            manager.ConnectComponents(body, loop, 0, 1);   // back edge to loop's second input
            manager.ConnectComponents(loop, end, 1, 0);    // exit

            var code = new FlowchartCodeGenerator().Generate(manager.GetComponents(), manager.GetLines(), CodeLanguage.Pseudocode);

            Assert.Contains("WHILE i < 10 DO", code);
            Assert.Contains("work;", code);
            Assert.Contains("END WHILE", code);
        }

        [Fact]
        public void EmptyDiagram_ReturnsEmpty()
        {
            var manager = new DrawingManager();
            var code = new FlowchartCodeGenerator().Generate(manager.GetComponents(), manager.GetLines());
            Assert.Equal(string.Empty, code);
        }
    }
}
