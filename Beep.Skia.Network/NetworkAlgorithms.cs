using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Network
{
    /// <summary>
    /// PageRank scores keyed by node.
    /// </summary>
    public class PageRankResult
    {
        public Dictionary<NetworkNode, double> Ranks { get; } = new Dictionary<NetworkNode, double>();
        /// <summary>
        /// Gets or sets the iterations.
        /// </summary>
        public int Iterations { get; set; }
        /// <summary>
        /// Gets or sets the converged.
        /// </summary>
        public bool Converged { get; set; }

        /// <summary>
        /// Returns the highest-ranked nodes.
        /// </summary>
        public List<KeyValuePair<NetworkNode, double>> Top(int count)
            => Ranks.OrderByDescending(kvp => kvp.Value).Take(Math.Max(0, count)).ToList();
    }

    /// <summary>
    /// Graph algorithms over NetworkNode/NetworkLink diagrams: PageRank, shortest paths,
    /// all simple paths, connectivity, and connected components.
    /// Links are treated as directed unless <see cref="NetworkLink.Bidirectional"/> is set.
    /// </summary>
    public static class NetworkAlgorithms
    {
        // ── PageRank ─────────────────────────────────────────────────────────

        /// <summary>
        /// Computes PageRank scores. Dangling nodes distribute their rank evenly across all nodes.
        /// </summary>
        public static PageRankResult PageRank(
            IEnumerable<NetworkNode> nodes,
            IEnumerable<NetworkLink> links,
            double damping = 0.85,
            int maxIterations = 100,
            double tolerance = 1e-8)
        {
            var result = new PageRankResult();
            var nodeList = (nodes ?? Enumerable.Empty<NetworkNode>())
                .Where(n => n != null)
                .Distinct()
                .ToList();
            if (nodeList.Count == 0) return result;

            int count = nodeList.Count;
            var indexOf = new Dictionary<NetworkNode, int>();
            for (int i = 0; i < count; i++) indexOf[nodeList[i]] = i;

            var outbound = new List<int>[count];
            for (int i = 0; i < count; i++) outbound[i] = new List<int>();

            foreach (var link in links ?? Enumerable.Empty<NetworkLink>())
            {
                if (link?.SourceNode == null || link.TargetNode == null) continue;
                if (!indexOf.TryGetValue(link.SourceNode, out var source)) continue;
                if (!indexOf.TryGetValue(link.TargetNode, out var target)) continue;

                outbound[source].Add(target);
                if (link.Bidirectional && source != target) outbound[target].Add(source);
            }

            double initial = 1.0 / count;
            var rank = Enumerable.Repeat(initial, count).ToArray();

            for (int iteration = 0; iteration < Math.Max(1, maxIterations); iteration++)
            {
                var next = Enumerable.Repeat((1.0 - damping) / count, count).ToArray();

                for (int i = 0; i < count; i++)
                {
                    if (outbound[i].Count == 0)
                    {
                        // Dangling node: spread its rank evenly.
                        double share = damping * rank[i] / count;
                        for (int j = 0; j < count; j++) next[j] += share;
                        continue;
                    }

                    double perTarget = damping * rank[i] / outbound[i].Count;
                    foreach (var target in outbound[i]) next[target] += perTarget;
                }

                double delta = 0;
                for (int i = 0; i < count; i++) delta += Math.Abs(next[i] - rank[i]);
                rank = next;
                result.Iterations = iteration + 1;

                if (delta < tolerance)
                {
                    result.Converged = true;
                    break;
                }
            }

            for (int i = 0; i < count; i++) result.Ranks[nodeList[i]] = rank[i];
            return result;
        }

        /// <summary>
        /// Applies PageRank scores visually: node scale (and optional color alpha) are normalized
        /// between the minimum and maximum scores.
        /// </summary>
        public static void ApplyPageRank(PageRankResult result, SKColor? color = null)
        {
            if (result == null || result.Ranks.Count == 0) return;

            double min = result.Ranks.Values.Min();
            double max = result.Ranks.Values.Max();
            double range = max - min;

            foreach (var kvp in result.Ranks)
            {
                double normalized = range > 1e-12 ? (kvp.Value - min) / range : 0.5;
                kvp.Key.Scale = 0.75f + (float)normalized * 0.75f;

                if (color.HasValue)
                {
                    var alpha = (byte)Math.Max(60, Math.Min(255, 120 + normalized * 135));
                    kvp.Key.CentralityColor = color.Value.WithAlpha(alpha);
                }
            }
        }

        // ── Pathfinding ──────────────────────────────────────────────────────

        /// <summary>
        /// Finds the shortest path (Dijkstra when weighted, BFS otherwise).
        /// Returns the node sequence including endpoints, or an empty list when unreachable.
        /// </summary>
        public static List<NetworkNode> ShortestPath(
            NetworkNode from,
            NetworkNode to,
            IEnumerable<NetworkLink> links,
            bool weighted = true)
        {
            if (from == null || to == null) return new List<NetworkNode>();
            if (ReferenceEquals(from, to)) return new List<NetworkNode> { from };

            var adjacency = BuildAdjacency(new[] { from, to }, links, expand: true);
            if (!adjacency.ContainsKey(from) || !adjacency.ContainsKey(to)) return new List<NetworkNode>();

            var distances = new Dictionary<NetworkNode, double> { [from] = 0 };
            var previous = new Dictionary<NetworkNode, NetworkNode>();
            var visited = new HashSet<NetworkNode>();

            // Simple priority selection keeps the implementation dependency-free.
            while (true)
            {
                NetworkNode current = null;
                double best = double.PositiveInfinity;
                foreach (var kvp in distances)
                {
                    if (visited.Contains(kvp.Key)) continue;
                    if (kvp.Value < best) { best = kvp.Value; current = kvp.Key; }
                }
                if (current == null) break;
                if (ReferenceEquals(current, to)) break;

                visited.Add(current);

                foreach (var edge in adjacency[current])
                {
                    if (visited.Contains(edge.Target)) continue;
                    double cost = weighted ? EdgeWeight(edge.Link) : 1d;
                    double candidate = distances[current] + cost;

                    if (!distances.TryGetValue(edge.Target, out var existing) || candidate < existing)
                    {
                        distances[edge.Target] = candidate;
                        previous[edge.Target] = current;
                    }
                }
            }

            if (!previous.ContainsKey(to)) return new List<NetworkNode>();

            var path = new List<NetworkNode> { to };
            var cursor = to;
            while (previous.TryGetValue(cursor, out var parent))
            {
                path.Add(parent);
                cursor = parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Enumerates simple (loop-free) paths between two nodes, depth-first, up to a cap.
        /// </summary>
        public static List<List<NetworkNode>> AllSimplePaths(
            NetworkNode from,
            NetworkNode to,
            IEnumerable<NetworkLink> links,
            int maxPaths = 50,
            int maxDepth = 32)
        {
            var results = new List<List<NetworkNode>>();
            if (from == null || to == null) return results;

            var adjacency = BuildAdjacency(new[] { from, to }, links, expand: true);
            var path = new List<NetworkNode>();
            var visited = new HashSet<NetworkNode>();

            void Walk(NetworkNode current, int depth)
            {
                if (results.Count >= maxPaths || depth > maxDepth) return;

                path.Add(current);
                visited.Add(current);

                if (ReferenceEquals(current, to))
                {
                    results.Add(new List<NetworkNode>(path));
                }
                else if (adjacency.TryGetValue(current, out var edges))
                {
                    foreach (var edge in edges)
                    {
                        if (visited.Contains(edge.Target)) continue;
                        Walk(edge.Target, depth + 1);
                        if (results.Count >= maxPaths) break;
                    }
                }

                visited.Remove(current);
                path.RemoveAt(path.Count - 1);
            }

            Walk(from, 0);
            return results;
        }

        /// <summary>
        /// Returns true when all nodes are reachable from the first node (ignoring link direction).
        /// </summary>
        public static bool IsConnected(IEnumerable<NetworkNode> nodes, IEnumerable<NetworkLink> links)
        {
            var nodeList = (nodes ?? Enumerable.Empty<NetworkNode>()).Where(n => n != null).Distinct().ToList();
            if (nodeList.Count <= 1) return true;

            var adjacency = BuildAdjacency(nodeList, links, expand: true);
            var visited = new HashSet<NetworkNode>();
            var queue = new Queue<NetworkNode>();
            queue.Enqueue(nodeList[0]);
            visited.Add(nodeList[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!adjacency.TryGetValue(current, out var edges)) continue;
                foreach (var edge in edges)
                {
                    if (visited.Add(edge.Target)) queue.Enqueue(edge.Target);
                }
            }

            return visited.Count == nodeList.Count;
        }

        /// <summary>
        /// Returns connected components (undirected view of the graph).
        /// </summary>
        public static List<List<NetworkNode>> ConnectedComponents(IEnumerable<NetworkNode> nodes, IEnumerable<NetworkLink> links)
        {
            var nodeList = (nodes ?? Enumerable.Empty<NetworkNode>()).Where(n => n != null).Distinct().ToList();
            var components = new List<List<NetworkNode>>();
            if (nodeList.Count == 0) return components;

            var adjacency = BuildAdjacency(nodeList, links, expand: true);
            var visited = new HashSet<NetworkNode>();

            foreach (var start in nodeList)
            {
                if (visited.Contains(start)) continue;

                var component = new List<NetworkNode>();
                var queue = new Queue<NetworkNode>();
                queue.Enqueue(start);
                visited.Add(start);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(current);
                    if (!adjacency.TryGetValue(current, out var edges)) continue;
                    foreach (var edge in edges)
                    {
                        if (visited.Add(edge.Target)) queue.Enqueue(edge.Target);
                    }
                }

                components.Add(component);
            }

            return components;
        }

        /// <summary>
        /// Sums the cost of a node path (edges matched by consecutive node pairs).
        /// </summary>
        public static double PathCost(IReadOnlyList<NetworkNode> path, IEnumerable<NetworkLink> links, bool weighted = true)
        {
            if (path == null || path.Count < 2) return 0d;

            var lookup = BuildEdgeLookup(links);
            double cost = 0d;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var key = (path[i], path[i + 1]);
                if (lookup.TryGetValue(key, out var link))
                    cost += weighted ? EdgeWeight(link) : 1d;
                else
                    cost += 1d;
            }
            return cost;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private sealed class Edge
        {
            public NetworkNode Target;
            public NetworkLink Link;
        }

        private static Dictionary<NetworkNode, List<Edge>> BuildAdjacency(
            IEnumerable<NetworkNode> nodes,
            IEnumerable<NetworkLink> links,
            bool expand)
        {
            var adjacency = new Dictionary<NetworkNode, List<Edge>>();

            void Ensure(NetworkNode node)
            {
                if (node != null && !adjacency.ContainsKey(node))
                    adjacency[node] = new List<Edge>();
            }

            foreach (var node in nodes ?? Enumerable.Empty<NetworkNode>()) Ensure(node);

            foreach (var link in links ?? Enumerable.Empty<NetworkLink>())
            {
                if (link?.SourceNode == null || link.TargetNode == null) continue;

                if (expand)
                {
                    Ensure(link.SourceNode);
                    Ensure(link.TargetNode);
                }
                else if (!adjacency.ContainsKey(link.SourceNode) || !adjacency.ContainsKey(link.TargetNode))
                {
                    continue;
                }

                adjacency[link.SourceNode].Add(new Edge { Target = link.TargetNode, Link = link });
                if (link.Bidirectional && !ReferenceEquals(link.SourceNode, link.TargetNode))
                    adjacency[link.TargetNode].Add(new Edge { Target = link.SourceNode, Link = link });
            }

            return adjacency;
        }

        private static Dictionary<(NetworkNode, NetworkNode), NetworkLink> BuildEdgeLookup(IEnumerable<NetworkLink> links)
        {
            var lookup = new Dictionary<(NetworkNode, NetworkNode), NetworkLink>();
            foreach (var link in links ?? Enumerable.Empty<NetworkLink>())
            {
                if (link?.SourceNode == null || link.TargetNode == null) continue;
                lookup[(link.SourceNode, link.TargetNode)] = link;
                if (link.Bidirectional) lookup[(link.TargetNode, link.SourceNode)] = link;
            }
            return lookup;
        }

        private static double EdgeWeight(NetworkLink link)
            => link != null && link.Weight > 0 ? link.Weight : 1d;
    }
}
