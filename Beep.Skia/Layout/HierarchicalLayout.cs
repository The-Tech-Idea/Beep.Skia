using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.Layout
{
    /// <summary>
    /// Layered (Sugiyama-style) layout for flowcharts and other directed diagrams. Cycles are broken by reversing the
    /// back edges of a depth-first search; nodes take the longest-path layer (sources are pulled next to their
    /// successors); long edges pass through placeholder nodes so that layer orders are chosen by barycentre sweeps that
    /// keep the order with the fewest crossings; positions along each layer are the closest to the node's neighbours that
    /// keep the spacing (weighted isotonic regression). Separate graphs are placed side by side. Deterministic: the same
    /// input always gives the same layout.
    /// </summary>
    public class HierarchicalLayout : IAutoLayout
    {
        public const int MaxNodes = 10_000;
        public const int MaxEdges = 40_000;
        /// <summary>Placeholder nodes for long edges, across the whole layout. Beyond it the remaining long edges are left
        /// out of the ordering (they are still drawn), which keeps very deep graphs bounded.</summary>
        public const int MaxPlaceholders = 40_000;
        private const int OrderingSweeps = 24;
        private const int PlacementSweeps = 8;

        /// <summary>The direction in which layers follow each other.</summary>
        public LayoutDirection Direction { get; set; } = LayoutDirection.TopToBottom;
        /// <summary>The space between neighbouring layers.</summary>
        public float LayerSpacing { get; set; } = 80f;
        /// <summary>The space between neighbouring nodes in a layer; separate graphs are twice as far apart.</summary>
        public float NodeSpacing { get; set; } = 48f;
        /// <summary>The left of the layout.</summary>
        public float StartX { get; set; } = 50f;
        /// <summary>The top of the layout.</summary>
        public float StartY { get; set; } = 50f;

        /// <summary>Moves the non-static components into layers along the lines that join them.</summary>
        public void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null || components.Count == 0) return;
            var nodes = components.Where(c => !c.IsStatic).ToList();
            if (nodes.Count == 0) return;
            var index = new Dictionary<SkiaComponent, int>();
            for (int i = 0; i < nodes.Count; i++) index[nodes[i]] = i;
            var edges = new List<(int From, int To)>();
            foreach (var line in lines ?? Array.Empty<IConnectionLine>())
                if (line?.Start?.Component is SkiaComponent from && line.End?.Component is SkiaComponent to
                    && index.TryGetValue(from, out var f) && index.TryGetValue(to, out var t))
                    edges.Add((f, t));
            var positions = Layout(nodes.Select(c => new SKSize(c.Width, c.Height)).ToArray(), edges);
            for (int i = 0; i < nodes.Count; i++) { nodes[i].X = positions[i].X; nodes[i].Y = positions[i].Y; }
        }

        /// <summary>
        /// The top-left corner for each box (by index) of the given sizes, with edges given as (from, to) index pairs.
        /// Self edges are allowed and ignored.
        /// </summary>
        public SKPoint[] Layout(IReadOnlyList<SKSize> sizes, IReadOnlyList<(int From, int To)> edges)
        {
            if (sizes == null) throw new ArgumentNullException(nameof(sizes));
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            if (sizes.Count > MaxNodes) throw new ArgumentException($"A layout takes at most {MaxNodes} nodes.", nameof(sizes));
            if (edges.Count > MaxEdges) throw new ArgumentException($"A layout takes at most {MaxEdges} edges.", nameof(edges));
            if (!float.IsFinite(LayerSpacing) || LayerSpacing < 0 || !float.IsFinite(NodeSpacing) || NodeSpacing < 0 || !float.IsFinite(StartX) || !float.IsFinite(StartY))
                throw new InvalidOperationException("Layout spacing and start must be finite, and spacing not negative.");
            int n = sizes.Count;
            foreach (var size in sizes)
                if (!float.IsFinite(size.Width) || !float.IsFinite(size.Height) || size.Width < 0 || size.Height < 0)
                    throw new ArgumentException("Node sizes must be finite and not negative.", nameof(sizes));
            foreach (var (from, to) in edges)
                if ((uint)from >= (uint)n || (uint)to >= (uint)n) throw new ArgumentException("An edge refers to a node that does not exist.", nameof(edges));
            var result = new SKPoint[n];
            if (n == 0) return result;

            bool vertical = Direction switch
            {
                LayoutDirection.TopToBottom => true, LayoutDirection.BottomToTop => true,
                LayoutDirection.LeftToRight => false, LayoutDirection.RightToLeft => false,
            };
            var breadth = sizes.Select(s => vertical ? s.Width : s.Height).ToArray();
            var depth = sizes.Select(s => vertical ? s.Height : s.Width).ToArray();
            var budget = new Budget { Placeholders = MaxPlaceholders };

            // Separate graphs (weakly connected parts), each laid out alone, then side by side in order of first node.
            var parent = Enumerable.Range(0, n).ToArray();
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            foreach (var (from, to) in edges) { var a = Find(from); var b = Find(to); if (a != b) parent[Math.Max(a, b)] = Math.Min(a, b); }
            var parts = Enumerable.Range(0, n).GroupBy(Find).OrderBy(g => g.Key).Select(g => g.ToArray()).ToList();
            var placedBreadth = new float[n]; var placedDepth = new float[n];
            float cursor = 0, totalDepth = 0;
            foreach (var part in parts)
            {
                var (b, d, partBreadth, partDepth) = LayoutPart(part, edges, breadth, depth, budget);
                for (int i = 0; i < part.Length; i++) { placedBreadth[part[i]] = cursor + b[i]; placedDepth[part[i]] = d[i]; }
                cursor += partBreadth + NodeSpacing * 2;
                totalDepth = Math.Max(totalDepth, partDepth);
            }
            for (int i = 0; i < n; i++)
            {
                var along = placedBreadth[i]; var down = placedDepth[i];
                result[i] = Direction switch
                {
                    LayoutDirection.TopToBottom => new SKPoint(StartX + along, StartY + down),
                    LayoutDirection.BottomToTop => new SKPoint(StartX + along, StartY + totalDepth - down - depth[i]),
                    LayoutDirection.LeftToRight => new SKPoint(StartX + down, StartY + along),
                    LayoutDirection.RightToLeft => new SKPoint(StartX + totalDepth - down - depth[i], StartY + along),
                };
            }
            return result;
        }

        private sealed class Budget { public int Placeholders; }

        /// <summary>Lays out one connected graph: returns the near edge of each node along its layer and across the layers
        /// (both from 0), with the part's extent.</summary>
        private (float[] Breadth, float[] Depth, float Width, float Height) LayoutPart(int[] part, IReadOnlyList<(int From, int To)> allEdges,
            float[] breadthOf, float[] depthOf, Budget budget)
        {
            int n = part.Length;
            var local = new Dictionary<int, int>(n);
            for (int i = 0; i < n; i++) local[part[i]] = i;
            var edges = new List<(int From, int To)>();
            foreach (var (from, to) in allEdges)
                if (from != to && local.TryGetValue(from, out var f) && local.TryGetValue(to, out var t)) edges.Add((f, t));

            // 1. Break cycles: reverse each edge that a depth-first search finds pointing back into its own path.
            var outgoing = new List<int>[n];
            for (int i = 0; i < n; i++) outgoing[i] = new List<int>();
            for (int e = 0; e < edges.Count; e++) outgoing[edges[e].From].Add(e);
            var state = new byte[n]; var reversed = new bool[edges.Count];
            var stack = new (int Node, int Next)[n]; int top;
            for (int s = 0; s < n; s++)
            {
                if (state[s] != 0) continue;
                top = 0; stack[top++] = (s, 0); state[s] = 1;
                while (top > 0)
                {
                    var (v, next) = stack[top - 1];
                    if (next < outgoing[v].Count)
                    {
                        stack[top - 1] = (v, next + 1);
                        var e = outgoing[v][next]; var w = edges[e].To;
                        if (state[w] == 1) reversed[e] = true;
                        else if (state[w] == 0) { state[w] = 1; stack[top++] = (w, 0); }
                    }
                    else { state[v] = 2; top--; }
                }
            }
            var dag = edges.Select((e, i) => reversed[i] ? (From: e.To, To: e.From) : (From: e.From, To: e.To)).ToList();

            // 2. Layers: longest path from the sources, then each source moves down to just above its nearest successor.
            var succ = new List<int>[n]; var pred = new List<int>[n];
            for (int i = 0; i < n; i++) { succ[i] = new List<int>(); pred[i] = new List<int>(); }
            foreach (var (from, to) in dag) { succ[from].Add(to); pred[to].Add(from); }
            var indegree = pred.Select(p => p.Count).ToArray();
            var order = new List<int>(n); var ready = new Queue<int>();
            for (int i = 0; i < n; i++) if (indegree[i] == 0) ready.Enqueue(i);
            while (ready.Count > 0) { var v = ready.Dequeue(); order.Add(v); foreach (var w in succ[v]) if (--indegree[w] == 0) ready.Enqueue(w); }
            var layer = new int[n];
            foreach (var v in order) foreach (var w in succ[v]) layer[w] = Math.Max(layer[w], layer[v] + 1);
            for (int k = order.Count - 1; k >= 0; k--)
            {
                var v = order[k];
                if (pred[v].Count == 0 && succ[v].Count > 0) layer[v] = succ[v].Min(w => layer[w]) - 1;
            }
            var first = layer.Min();
            for (int i = 0; i < n; i++) layer[i] -= first;
            int layers = layer.Max() + 1;

            // 3. Placeholders on long edges, so that orders account for the edges passing through a layer.
            var nodeLayer = new List<int>(layer); var isReal = Enumerable.Repeat(true, n).ToList();
            var segments = new List<(int Upper, int Lower)>();
            foreach (var (from, to) in dag)
            {
                var span = layer[to] - layer[from];
                if (span == 1) { segments.Add((from, to)); continue; }
                if (span - 1 > budget.Placeholders) continue; // Out of budget: drawn, but not considered in ordering.
                budget.Placeholders -= span - 1;
                var previous = from;
                for (int l = layer[from] + 1; l < layer[to]; l++)
                {
                    var dummy = nodeLayer.Count; nodeLayer.Add(l); isReal.Add(false);
                    segments.Add((previous, dummy)); previous = dummy;
                }
                segments.Add((previous, to));
            }
            int total = nodeLayer.Count;
            var up = new List<int>[total]; var down = new List<int>[total];
            for (int i = 0; i < total; i++) { up[i] = new List<int>(); down[i] = new List<int>(); }
            foreach (var (upper, lower) in segments) { down[upper].Add(lower); up[lower].Add(upper); }

            // 4. Orders: start from the order of first appearance, then barycentre sweeps down and up, keeping the best.
            var rows = new List<int>[layers];
            for (int l = 0; l < layers; l++) rows[l] = new List<int>();
            for (int i = 0; i < total; i++) rows[nodeLayer[i]].Add(i);
            var position = new double[total];
            void Number() { foreach (var row in rows) for (int i = 0; i < row.Count; i++) position[row[i]] = i; }
            Number();
            for (int l = 1; l < layers; l++) { SortBy(rows[l], up, position); for (int i = 0; i < rows[l].Count; i++) position[rows[l][i]] = i; }
            var best = rows.Select(r => r.ToArray()).ToArray(); long bestCrossings = Crossings(rows, down, total);
            int stale = 0;
            for (int sweep = 0; sweep < OrderingSweeps && bestCrossings > 0 && stale < 4; sweep++)
            {
                for (int l = 1; l < layers; l++) { SortBy(rows[l], up, position); for (int i = 0; i < rows[l].Count; i++) position[rows[l][i]] = i; }
                for (int l = layers - 2; l >= 0; l--) { SortBy(rows[l], down, position); for (int i = 0; i < rows[l].Count; i++) position[rows[l][i]] = i; }
                var crossings = Crossings(rows, down, total);
                if (crossings < bestCrossings) { bestCrossings = crossings; best = rows.Select(r => r.ToArray()).ToArray(); stale = 0; }
                else stale++;
            }
            for (int l = 0; l < layers; l++) rows[l] = best[l].ToList();

            // 5. Positions along the layers: pack, then repeatedly move each node towards its neighbours' centre.
            float Width(int v) => v < n ? breadthOf[part[v]] : 0f;
            float Gap(int a, int b) => (Width(a) + Width(b)) / 2f + (isReal[a] && isReal[b] ? NodeSpacing : NodeSpacing / 2f);
            var centre = new double[total];
            foreach (var row in rows)
            {
                double x = 0;
                for (int i = 0; i < row.Count; i++) { if (i > 0) x += Gap(row[i - 1], row[i]); centre[row[i]] = x; }
                var shift = row.Count == 0 ? 0 : (centre[row[0]] + centre[row[^1]]) / 2;
                foreach (var v in row) centre[v] -= shift;
            }
            for (int sweep = 0; sweep < PlacementSweeps; sweep++)
            {
                var downward = sweep % 2 == 0;
                for (int k = 1; k < layers; k++)
                {
                    var l = downward ? k : layers - 1 - k;
                    Place(rows[l], downward ? up : down, centre, Gap, isReal);
                }
            }
            // A last pass towards both neighbours, which balances nodes between their parents and children.
            for (int l = 0; l < layers; l++) Place(rows[l], null, centre, Gap, isReal, up, down);

            // 6. Extents: from the near edge of the outermost node, and layers stacked with their spacing.
            double minEdge = double.MaxValue, maxEdge = double.MinValue;
            for (int v = 0; v < total; v++) { minEdge = Math.Min(minEdge, centre[v] - Width(v) / 2); maxEdge = Math.Max(maxEdge, centre[v] + Width(v) / 2); }
            var thickness = new float[layers];
            for (int v = 0; v < n; v++) thickness[layer[v]] = Math.Max(thickness[layer[v]], depthOf[part[v]]);
            var layerStart = new float[layers]; float at = 0;
            for (int l = 0; l < layers; l++) { layerStart[l] = at; at += thickness[l] + (l < layers - 1 ? LayerSpacing : 0); }
            var breadth = new float[n]; var depth = new float[n];
            for (int v = 0; v < n; v++)
            {
                breadth[v] = (float)(centre[v] - Width(v) / 2 - minEdge);
                depth[v] = layerStart[layer[v]] + (thickness[layer[v]] - depthOf[part[v]]) / 2;
            }
            return (breadth, depth, (float)(maxEdge - minEdge), at);
        }

        /// <summary>Orders a layer by the mean position of its neighbours in the other layer; a node without neighbours
        /// keeps its place. The sort is stable, so ties keep the current order.</summary>
        private static void SortBy(List<int> row, List<int>[] neighbours, double[] position)
        {
            var keys = new Dictionary<int, double>(row.Count);
            foreach (var v in row) keys[v] = neighbours[v].Count == 0 ? position[v] : neighbours[v].Average(w => position[w]);
            var sorted = row.OrderBy(v => keys[v]).ToList();
            row.Clear(); row.AddRange(sorted);
        }

        /// <summary>Crossings between every pair of neighbouring layers (Barth, Jünger and Mutzel's accumulator count).</summary>
        private static long Crossings(List<int>[] rows, List<int>[] down, int total)
        {
            var index = new int[total];
            foreach (var row in rows) for (int i = 0; i < row.Count; i++) index[row[i]] = i;
            long crossings = 0;
            for (int l = 0; l + 1 < rows.Length; l++)
            {
                var targets = new List<int>();
                foreach (var v in rows[l]) targets.AddRange(down[v].Select(w => index[w]).OrderBy(i => i));
                var size = rows[l + 1].Count; if (size == 0 || targets.Count < 2) continue;
                var tree = new int[size + 1];
                for (int k = 0; k < targets.Count; k++)
                {
                    // Edges already counted that end to the right of this one cross it.
                    int seen = 0; for (int i = targets[k] + 1; i > 0; i -= i & -i) seen += tree[i];
                    crossings += k - seen;
                    for (int i = targets[k] + 1; i <= size; i += i & -i) tree[i]++;
                }
            }
            return crossings;
        }

        /// <summary>
        /// Moves a layer's nodes as close as possible to where their neighbours want them while keeping their order and
        /// spacing: weighted isotonic regression (pool adjacent violators) on positions less the spacing before them.
        /// Placeholders weigh more, which keeps long edges straight.
        /// </summary>
        private static void Place(List<int> row, List<int>[] neighbours, double[] centre, Func<int, int, float> gap, List<bool> isReal,
            List<int>[] up = null, List<int>[] down = null)
        {
            if (row.Count == 0) return;
            var offset = new double[row.Count];
            for (int i = 1; i < row.Count; i++) offset[i] = offset[i - 1] + gap(row[i - 1], row[i]);
            var blockSum = new double[row.Count]; var blockWeight = new double[row.Count]; var blockCount = new int[row.Count]; int blocks = 0;
            for (int i = 0; i < row.Count; i++)
            {
                var v = row[i];
                IEnumerable<int> near = neighbours is not null ? neighbours[v] : up[v].Concat(down[v]);
                double desired = near.Any() ? near.Average(w => centre[w]) : centre[v];
                double weight = isReal[v] ? 1 : 2;
                blockSum[blocks] = (desired - offset[i]) * weight; blockWeight[blocks] = weight; blockCount[blocks] = 1; blocks++;
                while (blocks > 1 && blockSum[blocks - 2] / blockWeight[blocks - 2] > blockSum[blocks - 1] / blockWeight[blocks - 1])
                {
                    blockSum[blocks - 2] += blockSum[blocks - 1]; blockWeight[blocks - 2] += blockWeight[blocks - 1]; blockCount[blocks - 2] += blockCount[blocks - 1];
                    blocks--;
                }
            }
            int at = 0;
            for (int b = 0; b < blocks; b++)
            {
                var value = blockSum[b] / blockWeight[b];
                for (int k = 0; k < blockCount[b]; k++, at++) centre[row[at]] = value + offset[at];
            }
        }
    }
}
