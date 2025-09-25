using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Centrality measure component that visualizes node importance in the network.
    /// Supports different centrality algorithms and displays rankings.
    /// </summary>
    public class CentralityMeasure : NetworkControl
    {
        /// <summary>
        /// Gets or sets the type of centrality measure to display.
        /// </summary>
        public CentralityType MeasureType { get; set; } = CentralityType.Degree;

        /// <summary>
        /// Gets the list of nodes with their centrality scores.
        /// </summary>
        public List<NodeCentrality> NodeCentralities { get; } = new List<NodeCentrality>();

        /// <summary>
        /// Gets or sets the maximum number of top nodes to display.
        /// </summary>
        public int MaxDisplayCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to show centrality values.
        /// </summary>
        public bool ShowValues { get; set; } = true;

        /// <summary>
        /// Gets or sets the color for high centrality nodes.
        /// </summary>
        public SKColor HighCentralityColor { get; set; } = new SKColor(0x4C, 0xAF, 0x50); // Green

        /// <summary>
        /// Gets or sets the color for low centrality nodes.
        /// </summary>
        public SKColor LowCentralityColor { get; set; } = new SKColor(0xF4, 0x43, 0x36); // Red

        /// <summary>
        /// Gets or sets the color for medium centrality nodes.
        /// </summary>
        public SKColor MediumCentralityColor { get; set; } = new SKColor(0xFF, 0x98, 0x00); // Orange

        /// <summary>
        /// Initializes a new instance of the <see cref="CentralityMeasure"/> class.
        /// </summary>
        public CentralityMeasure()
        {
            Width = 280;
            Height = 200;
            Name = "CentralityMeasure";
            DisplayText = "Node Centrality";
            TextPosition = TextPosition.Above;
            PrimaryColor = new SKColor(0x4C, 0xAF, 0x50); // Green
        }

        /// <summary>
        /// Calculates centrality measures for the given network.
        /// </summary>
        /// <param name="nodes">The network nodes.</param>
        /// <param name="links">The network links.</param>
        public void CalculateCentrality(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            NodeCentralities.Clear();

            if (nodes == null || nodes.Count == 0)
                return;

            switch (MeasureType)
            {
                case CentralityType.Degree:
                    CalculateDegreeCentrality(nodes, links);
                    break;
                case CentralityType.Betweenness:
                    CalculateBetweennessCentrality(nodes, links);
                    break;
                case CentralityType.Closeness:
                    CalculateClosenessCentrality(nodes, links);
                    break;
                case CentralityType.Eigenvector:
                    CalculateEigenvectorCentrality(nodes, links);
                    break;
            }

            // Sort by centrality score descending
            NodeCentralities.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Apply visual scaling to nodes
            ApplyVisualScaling();
        }

        /// <summary>
        /// Calculates degree centrality (number of connections).
        /// </summary>
        private void CalculateDegreeCentrality(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var degreeMap = new Dictionary<NetworkNode, int>();

            // Initialize degrees
            foreach (var node in nodes)
            {
                degreeMap[node] = 0;
            }

            // Count connections
            foreach (var link in links)
            {
                if (degreeMap.ContainsKey(link.SourceNode))
                    degreeMap[link.SourceNode]++;
                if (degreeMap.ContainsKey(link.TargetNode))
                    degreeMap[link.TargetNode]++;
            }

            // Create centrality objects
            foreach (var kvp in degreeMap)
            {
                NodeCentralities.Add(new NodeCentrality
                {
                    Node = kvp.Key,
                    Score = kvp.Value,
                    NormalizedScore = kvp.Value / (double)Math.Max(1, nodes.Count - 1)
                });
            }
        }

        /// <summary>
        /// Calculates betweenness centrality (control of information flow).
        /// </summary>
        private void CalculateBetweennessCentrality(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            // Simplified betweenness calculation
            // In a full implementation, this would use Floyd-Warshall or similar algorithm
            foreach (var node in nodes)
            {
                double betweenness = 0;
                // Count shortest paths that pass through this node
                // This is a placeholder - real implementation would be more complex
                NodeCentralities.Add(new NodeCentrality
                {
                    Node = node,
                    Score = betweenness,
                    NormalizedScore = betweenness / Math.Max(1, (nodes.Count - 1) * (nodes.Count - 2) / 2.0)
                });
            }
        }

        /// <summary>
        /// Calculates closeness centrality (average distance to other nodes).
        /// </summary>
        private void CalculateClosenessCentrality(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            // Simplified closeness calculation
            foreach (var node in nodes)
            {
                // Calculate average shortest path distance
                double totalDistance = 0;
                int reachableNodes = 0;

                foreach (var otherNode in nodes.Where(n => n != node))
                {
                    double distance = CalculateShortestPathDistance(node, otherNode, links);
                    if (distance < double.MaxValue)
                    {
                        totalDistance += distance;
                        reachableNodes++;
                    }
                }

                double closeness = reachableNodes > 0 ? reachableNodes / totalDistance : 0;
                NodeCentralities.Add(new NodeCentrality
                {
                    Node = node,
                    Score = closeness,
                    NormalizedScore = closeness
                });
            }
        }

        /// <summary>
        /// Calculates eigenvector centrality (importance based on connections to important nodes).
        /// </summary>
        private void CalculateEigenvectorCentrality(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            // Simplified eigenvector calculation using power iteration
            var centralityMap = new Dictionary<NetworkNode, double>();
            var adjacencyMatrix = BuildAdjacencyMatrix(nodes, links);

            // Initialize with equal values
            foreach (var node in nodes)
            {
                centralityMap[node] = 1.0 / nodes.Count;
            }

            // Power iteration (simplified)
            for (int iteration = 0; iteration < 10; iteration++)
            {
                var newCentrality = new Dictionary<NetworkNode, double>();

                foreach (var node in nodes)
                {
                    double sum = 0;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (adjacencyMatrix[nodes.IndexOf(node), i] > 0)
                        {
                            sum += centralityMap[nodes[i]];
                        }
                    }
                    newCentrality[node] = sum;
                }

                centralityMap = newCentrality;
            }

            // Normalize
            double maxCentrality = centralityMap.Values.Max();
            foreach (var kvp in centralityMap)
            {
                NodeCentralities.Add(new NodeCentrality
                {
                    Node = kvp.Key,
                    Score = kvp.Value,
                    NormalizedScore = maxCentrality > 0 ? kvp.Value / maxCentrality : 0
                });
            }
        }

        /// <summary>
        /// Builds adjacency matrix for eigenvector centrality calculation.
        /// </summary>
        private double[,] BuildAdjacencyMatrix(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var matrix = new double[nodes.Count, nodes.Count];

            foreach (var link in links)
            {
                int sourceIndex = nodes.IndexOf(link.SourceNode);
                int targetIndex = nodes.IndexOf(link.TargetNode);

                if (sourceIndex >= 0 && targetIndex >= 0)
                {
                    matrix[sourceIndex, targetIndex] = 1;
                    matrix[targetIndex, sourceIndex] = 1; // Undirected
                }
            }

            return matrix;
        }

        /// <summary>
        /// Calculates shortest path distance between two nodes.
        /// </summary>
        private double CalculateShortestPathDistance(NetworkNode start, NetworkNode end, List<NetworkLink> links)
        {
            // Simplified BFS for shortest path
            var distances = new Dictionary<NetworkNode, double>();
            var queue = new Queue<NetworkNode>();
            var visited = new HashSet<NetworkNode>();

            distances[start] = 0;
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                    return distances[current];

                foreach (var link in links.Where(l => l.SourceNode == current || l.TargetNode == current))
                {
                    var neighbor = link.SourceNode == current ? link.TargetNode : link.SourceNode;

                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        distances[neighbor] = distances[current] + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return double.MaxValue; // No path found
        }

        /// <summary>
        /// Applies visual scaling to nodes based on their centrality scores.
        /// </summary>
        private void ApplyVisualScaling()
        {
            if (NodeCentralities.Count == 0)
                return;

            double maxScore = NodeCentralities.Max(nc => nc.NormalizedScore);
            double minScore = NodeCentralities.Min(nc => nc.NormalizedScore);

            foreach (var centrality in NodeCentralities)
            {
                // Scale node size based on centrality (0.5x to 2x normal size)
                double scale = 0.5 + (centrality.NormalizedScore - minScore) / (maxScore - minScore) * 1.5;
                centrality.Node.Scale = (float)scale;

                // Color based on centrality level
                centrality.Node.CentralityColor = GetCentralityColor(centrality.NormalizedScore);
            }
        }

        /// <summary>
        /// Gets the color for a centrality score.
        /// </summary>
        private SKColor GetCentralityColor(double normalizedScore)
        {
            if (normalizedScore >= 0.7)
                return HighCentralityColor;
            else if (normalizedScore >= 0.4)
                return MediumCentralityColor;
            else
                return LowCentralityColor;
        }

        /// <summary>
        /// Draws the centrality measure control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, MaterialColors.SurfaceContainer);

            // Draw title
            float currentY = Y + 15;
            float lineHeight = 16;
            float leftMargin = X + 10;

            using var titleFont = new SKFont { Size = 14 };
            using var titlePaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
            canvas.DrawText($"{MeasureType} Centrality", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, titleFont, titlePaint);

            currentY += lineHeight + 5;

            // Draw top nodes
            using var font = new SKFont { Size = 11 };
            using var namePaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var valuePaint = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true };

            int displayCount = Math.Min(MaxDisplayCount, NodeCentralities.Count);
            for (int i = 0; i < displayCount; i++)
            {
                var centrality = NodeCentralities[i];
                string rankText = $"{i + 1}. {centrality.Node.Name}";
                string valueText = ShowValues ? $"{centrality.Score:F3}" : "";

                canvas.DrawText(rankText, leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, namePaint);

                if (ShowValues)
                {
                    canvas.DrawText(valueText, X + Width - 10, currentY + lineHeight - 3, SKTextAlign.Right, font, valuePaint);
                }

                currentY += lineHeight;
            }
        }
    }

    /// <summary>
    /// Represents centrality information for a network node.
    /// </summary>
    public class NodeCentrality
    {
        /// <summary>
        /// Gets or sets the network node.
        /// </summary>
        public NetworkNode Node { get; set; }

        /// <summary>
        /// Gets or sets the raw centrality score.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Gets or sets the normalized centrality score (0-1).
        /// </summary>
        public double NormalizedScore { get; set; }
    }

    /// <summary>
    /// Types of centrality measures.
    /// </summary>
    public enum CentralityType
    {
        /// <summary>
        /// Degree centrality (number of connections).
        /// </summary>
        Degree,

        /// <summary>
        /// Betweenness centrality (control of information flow).
        /// </summary>
        Betweenness,

        /// <summary>
        /// Closeness centrality (average distance to other nodes).
        /// </summary>
        Closeness,

        /// <summary>
        /// Eigenvector centrality (importance based on connections to important nodes).
        /// </summary>
        Eigenvector
    }
}