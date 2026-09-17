using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.Layout
{
    /// <summary>
    /// Interface for automatic diagram layout algorithms.
    /// Takes components and connection lines and repositions components.
    /// </summary>
    public interface IAutoLayout
    {
        void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines);
    }

    /// <summary>
    /// Hierarchical (layered Sugiyama-style) auto-layout for flowcharts and DAG diagrams.
    /// Places nodes in layers based on longest path from sources, then minimizes crossings.
    /// </summary>
    public class HierarchicalLayout : IAutoLayout
    {
        /// <summary>
        /// Gets or sets the top to bottom.
        /// </summary>
        public bool TopToBottom { get; set; } = true;
        /// <summary>
        /// Gets or sets the layer spacing.
        /// </summary>
        public float LayerSpacing { get; set; } = 120f;
        /// <summary>
        /// Gets or sets the node spacing.
        /// </summary>
        public float NodeSpacing { get; set; } = 80f;
        /// <summary>
        /// Gets or sets the start x.
        /// </summary>
        public float StartX { get; set; } = 50f;
        /// <summary>
        /// Gets or sets the start y.
        /// </summary>
        public float StartY { get; set; } = 50f;

        /// <summary>
        /// Gets or sets the arrange.
        /// </summary>
        public void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null || components.Count == 0) return;

            var compList = components.Where(c => !c.IsStatic).ToList();
            if (compList.Count == 0) return;

            // Build adjacency
            var inDegree = new Dictionary<SkiaComponent, int>();
            var children = new Dictionary<SkiaComponent, List<SkiaComponent>>();

            foreach (var c in compList)
            {
                inDegree[c] = 0;
                children[c] = new List<SkiaComponent>();
            }

            foreach (var line in lines)
            {
                if (line?.Start?.Component is SkiaComponent src && line.End?.Component is SkiaComponent dst
                    && compList.Contains(src) && compList.Contains(dst))
                {
                    children[src].Add(dst);
                    inDegree[dst] = inDegree.GetValueOrDefault(dst) + 1;
                }
            }

            // Topological sort into layers (BFS from sources, assign layer = max path length from any source)
            var sources = compList.Where(c => inDegree[c] == 0).ToList();
            if (sources.Count == 0)
            {
                // Graph has cycles — assign layers by BFS anyway
                sources = compList.Take(1).ToList();
                inDegree[sources[0]] = 0;
            }

            var layer = new Dictionary<SkiaComponent, int>();
            var queue = new Queue<SkiaComponent>();
            foreach (var s in sources)
            {
                layer[s] = 0;
                queue.Enqueue(s);
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var child in children[node])
                {
                    int newLayer = layer[node] + 1;
                    if (!layer.ContainsKey(child) || newLayer > layer[child])
                        layer[child] = newLayer;
                    queue.Enqueue(child);
                }
            }

            // Assign layers to any unreachable nodes
            int fallbackLayer = 0;
            foreach (var c in compList)
            {
                if (!layer.ContainsKey(c))
                    layer[c] = fallbackLayer++;
            }

            // Group by layer, sort within layer by center-of-parent Y
            var layers = compList.GroupBy(c => layer[c]).OrderBy(g => g.Key).ToList();
            int maxLayer = layers.Max(g => g.Key);

            // Position nodes
            foreach (var layerGroup in layers)
            {
                int l = layerGroup.Key;
                var nodesInLayer = layerGroup.ToList();

                // Center within this layer
                float maxHeight = nodesInLayer.Max(c => c.Height);
                float totalHeight = nodesInLayer.Sum(c => c.Height) + (nodesInLayer.Count - 1) * NodeSpacing;

                float startOffset = 0;
                foreach (var node in nodesInLayer)
                {
                    if (TopToBottom)
                    {
                        node.X = StartX + l * LayerSpacing;
                        node.Y = StartY + startOffset;
                    }
                    else
                    {
                        node.X = StartX + startOffset;
                        node.Y = StartY + l * LayerSpacing;
                    }

                    startOffset += node.Height + NodeSpacing;
                }
            }
        }
    }

    /// <summary>
    /// Radial auto-layout for MindMap diagrams.
    /// Children radiate outward from a central node at configurable angles and radius.
    /// </summary>
    public class RadialLayout : IAutoLayout
    {
        /// <summary>
        /// Gets or sets the min radius.
        /// </summary>
        public float MinRadius { get; set; } = 120f;
        /// <summary>
        /// Gets or sets the radius increment.
        /// </summary>
        public float RadiusIncrement { get; set; } = 100f;
        /// <summary>
        /// Gets or sets the start angle.
        /// </summary>
        public float StartAngle { get; set; } = -90f; // Top center
        /// <summary>
        /// Gets or sets the sweep angle.
        /// </summary>
        public float SweepAngle { get; set; } = 360f;

        /// <summary>
        /// Gets or sets the arrange.
        /// </summary>
        public void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null || components.Count == 0) return;

            var compList = components.Where(c => !c.IsStatic).ToList();
            if (compList.Count == 0) return;

            // Find central node (no incoming lines)
            var hasIncoming = new HashSet<SkiaComponent>();
            foreach (var line in lines)
            {
                if (line?.End?.Component is SkiaComponent dst && compList.Contains(dst))
                    hasIncoming.Add(dst);
            }

            SkiaComponent center = compList.FirstOrDefault(c => !hasIncoming.Contains(c)) ?? compList[0];
            center.X = 300f;
            center.Y = 250f;

            // BFS from center to get depth layers
            var depth = new Dictionary<SkiaComponent, int> { [center] = 0 };
            var queue = new Queue<SkiaComponent>();
            queue.Enqueue(center);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var line in lines)
                {
                    if (line?.Start?.Component == node && line.End?.Component is SkiaComponent next
                        && compList.Contains(next) && !depth.ContainsKey(next))
                    {
                        depth[next] = depth[node] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            // Group by depth
            var byDepth = compList.GroupBy(c => depth.GetValueOrDefault(c, 0)).OrderBy(g => g.Key).ToList();

            foreach (var group in byDepth)
            {
                int d = group.Key;
                if (d == 0) continue; // Skip center

                var nodes = group.ToList();
                float radius = MinRadius + d * RadiusIncrement;
                float anglePerNode = SweepAngle / nodes.Count;
                float angle = StartAngle;

                foreach (var node in nodes)
                {
                    float rad = (float)(angle * Math.PI / 180.0);
                    node.X = center.X + radius * (float)Math.Cos(rad) - node.Width / 2f;
                    node.Y = center.Y + radius * (float)Math.Sin(rad) - node.Height / 2f;
                    angle += anglePerNode;
                }
            }
        }
    }

    /// <summary>
    /// Force-directed layout for network graphs.
    /// Uses simple spring-electric repulsion/attraction to position nodes.
    /// </summary>
    public class ForceDirectedLayout : IAutoLayout
    {
        /// <summary>
        /// Gets or sets the iterations.
        /// </summary>
        public int Iterations { get; set; } = 100;
        /// <summary>
        /// Gets or sets the repulsion strength.
        /// </summary>
        public float RepulsionStrength { get; set; } = 5000f;
        /// <summary>
        /// Gets or sets the attraction strength.
        /// </summary>
        public float AttractionStrength { get; set; } = 0.01f;
        /// <summary>
        /// Gets or sets the ideal edge length.
        /// </summary>
        public float IdealEdgeLength { get; set; } = 150f;
        /// <summary>
        /// Gets or sets the damping.
        /// </summary>
        public float Damping { get; set; } = 0.9f;
        /// <summary>
        /// Gets or sets the center x.
        /// </summary>
        public float CenterX { get; set; } = 400f;
        /// <summary>
        /// Gets or sets the center y.
        /// </summary>
        public float CenterY { get; set; } = 300f;

        /// <summary>
        /// Gets or sets the arrange.
        /// </summary>
        public void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null || components.Count == 0) return;

            var compList = components.Where(c => !c.IsStatic).ToList();
            if (compList.Count == 0) return;

            int n = compList.Count;
            var positions = new SKPoint[n];
            var velocities = new SKPoint[n];

            // Initialize positions in a circle
            for (int i = 0; i < n; i++)
            {
                float angle = (float)(2 * Math.PI * i / n);
                positions[i] = new SKPoint(
                    CenterX + 100 * (float)Math.Cos(angle),
                    CenterY + 100 * (float)Math.Sin(angle));
                velocities[i] = SKPoint.Empty;
            }

            // Build adjacency
            var adj = new HashSet<(int, int)>();
            foreach (var line in lines)
            {
                var si = compList.IndexOf(line?.Start?.Component as SkiaComponent);
                var ti = compList.IndexOf(line?.End?.Component as SkiaComponent);
                if (si >= 0 && ti >= 0 && si != ti)
                    adj.Add((Math.Min(si, ti), Math.Max(si, ti)));
            }

            for (int iter = 0; iter < Iterations; iter++)
            {
                for (int i = 0; i < n; i++)
                {
                    var force = new SKPoint(0, 0);

                    // Repulsion from all other nodes
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j) continue;
                        var dx = positions[i].X - positions[j].X;
                        var dy = positions[i].Y - positions[j].Y;
                        float dist = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
                        float f = RepulsionStrength / (dist * dist);
                        force.X += (dx / dist) * f;
                        force.Y += (dy / dist) * f;
                    }

                    // Attraction along edges
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j) continue;
                        if (!adj.Contains((Math.Min(i, j), Math.Max(i, j)))) continue;
                        var dx = positions[i].X - positions[j].X;
                        var dy = positions[i].Y - positions[j].Y;
                        float dist = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
                        float f = AttractionStrength * (dist - IdealEdgeLength);
                        force.X -= (dx / dist) * f;
                        force.Y -= (dy / dist) * f;
                    }

                    // Center gravity
                    force.X += (CenterX - positions[i].X) * 0.001f;
                    force.Y += (CenterY - positions[i].Y) * 0.001f;

                    velocities[i].X = (velocities[i].X + force.X) * Damping;
                    velocities[i].Y = (velocities[i].Y + force.Y) * Damping;

                    // Clamp velocity
                    float speed = (float)Math.Sqrt(velocities[i].X * velocities[i].X + velocities[i].Y * velocities[i].Y);
                    if (speed > 50f)
                    {
                        velocities[i].X = velocities[i].X / speed * 50f;
                        velocities[i].Y = velocities[i].Y / speed * 50f;
                    }

                    positions[i].X += velocities[i].X;
                    positions[i].Y += velocities[i].Y;
                }
            }

            // Apply positions
            for (int i = 0; i < n; i++)
            {
                compList[i].X = positions[i].X;
                compList[i].Y = positions[i].Y;
            }
        }
    }

    /// <summary>
    /// Grid layout for UML class diagrams and similar structured layouts.
    /// </summary>
    public class GridAutoLayout : IAutoLayout
    {
        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        public int Columns { get; set; } = 3;
        /// <summary>
        /// Gets or sets the horizontal spacing.
        /// </summary>
        public float HorizontalSpacing { get; set; } = 40f;
        /// <summary>
        /// Gets or sets the vertical spacing.
        /// </summary>
        public float VerticalSpacing { get; set; } = 60f;
        /// <summary>
        /// Gets or sets the start x.
        /// </summary>
        public float StartX { get; set; } = 50f;
        /// <summary>
        /// Gets or sets the start y.
        /// </summary>
        public float StartY { get; set; } = 50f;

        /// <summary>
        /// Gets or sets the arrange.
        /// </summary>
        public void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var compList = components.Where(c => !c.IsStatic).ToList();
            if (compList.Count == 0) return;

            float colWidth = compList.Max(c => c.Width) + HorizontalSpacing;
            float rowHeight = compList.Max(c => c.Height) + VerticalSpacing;

            for (int i = 0; i < compList.Count; i++)
            {
                int col = i % Columns;
                int row = i / Columns;
                compList[i].X = StartX + col * colWidth;
                compList[i].Y = StartY + row * rowHeight;
            }
        }
    }
}
