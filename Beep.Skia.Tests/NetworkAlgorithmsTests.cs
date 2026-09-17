using System.Linq;
using Beep.Skia.Network;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="NetworkAlgorithms"/>: PageRank, shortest paths, simple paths,
    /// connectivity, components, and path cost.
    /// </summary>
    public class NetworkAlgorithmsTests
    {
        private static NetworkNode Node(string name) => new NetworkNode { Name = name };

        private static NetworkLink Link(NetworkNode source, NetworkNode target, double weight = 1, bool bidirectional = false)
            => new NetworkLink { SourceNode = source, TargetNode = target, Weight = weight, Bidirectional = bidirectional };

        [Fact]
        public void PageRank_FavoursNodesWithManyInboundLinks()
        {
            var hub = Node("hub");
            var l1 = Node("l1");
            var l2 = Node("l2");
            var l3 = Node("l3");
            var nodes = new[] { hub, l1, l2, l3 };
            var links = new[] { Link(l1, hub), Link(l2, hub), Link(l3, hub) };

            var result = NetworkAlgorithms.PageRank(nodes, links);

            Assert.Equal(4, result.Ranks.Count);
            Assert.Equal(hub, result.Top(1)[0].Key);
            Assert.True(result.Converged);
        }

        [Fact]
        public void PageRank_Cycle_HasEqualRanks()
        {
            var a = Node("a");
            var b = Node("b");
            var c = Node("c");
            var nodes = new[] { a, b, c };
            var links = new[] { Link(a, b), Link(b, c), Link(c, a) };

            var result = NetworkAlgorithms.PageRank(nodes, links);

            foreach (var rank in result.Ranks.Values)
                Assert.Equal(1.0 / 3.0, rank, 3);
        }

        [Fact]
        public void PageRank_BidirectionalLink_IsSymmetric()
        {
            var a = Node("a");
            var b = Node("b");
            var result = NetworkAlgorithms.PageRank(new[] { a, b }, new[] { Link(a, b, bidirectional: true) });

            Assert.Equal(result.Ranks[a], result.Ranks[b], 6);
        }

        [Fact]
        public void ApplyPageRank_SetsScaleAndColor()
        {
            var hub = Node("hub");
            var leaf = Node("leaf");
            var result = NetworkAlgorithms.PageRank(new[] { hub, leaf }, new[] { Link(leaf, hub) });

            NetworkAlgorithms.ApplyPageRank(result, SKColors.Orange);

            Assert.True(hub.Scale >= 0.75f && hub.Scale <= 1.5f);
            Assert.True(leaf.Scale >= 0.75f && leaf.Scale <= 1.5f);
            Assert.True(hub.CentralityColor.Alpha > 0);
        }

        [Fact]
        public void ShortestPath_Unweighted_PrefersFewerHops()
        {
            var a = Node("a");
            var b = Node("b");
            var c = Node("c");
            var links = new[] { Link(a, b), Link(b, c), Link(a, c, weight: 5) };

            var path = NetworkAlgorithms.ShortestPath(a, c, links, weighted: false);

            Assert.Equal(new[] { a, c }, path);
        }

        [Fact]
        public void ShortestPath_Weighted_PrefersLowerCost()
        {
            var a = Node("a");
            var b = Node("b");
            var c = Node("c");
            var links = new[] { Link(a, b), Link(b, c), Link(a, c, weight: 5) };

            var path = NetworkAlgorithms.ShortestPath(a, c, links, weighted: true);

            Assert.Equal(new[] { a, b, c }, path);
            Assert.Equal(2d, NetworkAlgorithms.PathCost(path, links));
        }

        [Fact]
        public void ShortestPath_Unreachable_ReturnsEmpty()
        {
            var a = Node("a");
            var b = Node("b");
            var isolated = Node("isolated");
            var path = NetworkAlgorithms.ShortestPath(a, isolated, new[] { Link(a, b) });

            Assert.Empty(path);
        }

        [Fact]
        public void AllSimplePaths_FindsBothSidesOfDiamond()
        {
            var a = Node("a");
            var b = Node("b");
            var c = Node("c");
            var d = Node("d");
            var links = new[] { Link(a, b), Link(a, c), Link(b, d), Link(c, d) };

            var paths = NetworkAlgorithms.AllSimplePaths(a, d, links);

            Assert.Equal(2, paths.Count);
            Assert.Contains(paths, p => p.SequenceEqual(new[] { a, b, d }));
            Assert.Contains(paths, p => p.SequenceEqual(new[] { a, c, d }));
        }

        [Fact]
        public void ConnectedComponents_SplitsDisconnectedGraph()
        {
            var a = Node("a");
            var b = Node("b");
            var c = Node("c");
            var d = Node("d");
            var links = new[] { Link(a, b, bidirectional: true), Link(c, d, bidirectional: true) };

            var components = NetworkAlgorithms.ConnectedComponents(new[] { a, b, c, d }, links);

            Assert.Equal(2, components.Count);
            Assert.Contains(components, comp => comp.Count == 2 && comp.Contains(a) && comp.Contains(b));
            Assert.Contains(components, comp => comp.Count == 2 && comp.Contains(c) && comp.Contains(d));
        }

        [Fact]
        public void IsConnected_DetectsBrokenGraph()
        {
            var a = Node("a");
            var b = Node("b");
            var c = Node("c");

            Assert.True(NetworkAlgorithms.IsConnected(new[] { a, b }, new[] { Link(a, b, bidirectional: true) }));
            Assert.False(NetworkAlgorithms.IsConnected(new[] { a, b, c }, new[] { Link(a, b, bidirectional: true) }));
        }
    }
}
