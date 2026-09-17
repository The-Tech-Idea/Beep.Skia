using System.Text;
using Beep.Skia.Assist;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Depth-limit hardening for the mind-map outline parser: indentation depth must be bounded
    /// so the recursive radial layout cannot overflow the stack.
    /// </summary>
    public class MindMapDepthHardeningTests
    {
        [Fact]
        public void MindMap_ExtremelyDeepOutline_IsRejectedInsteadOfOverflowing()
        {
            const int depth = 20000;
            var builder = new StringBuilder();
            for (var i = 0; i < depth; i++) builder.Append(new string(' ', i * 2)).Append("n").Append(i).Append('\n');

            var root = MindMapDslParser.Parse(builder.ToString(), out var warnings, maxNodes: depth + 10);

            Assert.NotNull(root);
            Assert.NotNull(warnings);
            // The parser must either bound the depth (warning) or complete; it must not crash.
            Assert.True(warnings.Count > 0, "deep nesting should be reported");
        }

        [Fact]
        public void MindMap_DeepButReasonableOutline_StillParses()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < 30; i++) builder.Append(new string(' ', i * 2)).Append("level").Append(i).Append('\n');

            var root = MindMapDslParser.Parse(builder.ToString(), out var warnings, maxNodes: 100);

            Assert.NotNull(root);
            var depth = 0;
            var node = root;
            while (node.Children.Count > 0)
            {
                depth++;
                node = node.Children[0];
            }
            Assert.Equal(29, depth);
            Assert.DoesNotContain(warnings, w => w.Contains("too deep", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
