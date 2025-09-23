using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Community detector component that identifies and visualizes network communities.
    /// Uses clustering algorithms to group related nodes and color-code them.
    /// </summary>
    public class CommunityDetector : NetworkControl
    {
        /// <summary>
        /// Gets or sets the algorithm used for community detection.
        /// </summary>
        public CommunityAlgorithm Algorithm { get; set; } = CommunityAlgorithm.Louvain;

        /// <summary>
        /// Gets the list of detected communities.
        /// </summary>
        public List<Community> Communities { get; } = new List<Community>();

        /// <summary>
        /// Gets or sets the minimum community size to display.
        /// </summary>
        public int MinCommunitySize { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to show community labels.
        /// </summary>
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets the color palette for communities.
        /// </summary>
        public List<SKColor> CommunityColors { get; set; } = new List<SKColor>
        {
            new SKColor(0xE8, 0xF5, 0xE8), // Light green
            new SKColor(0xE3, 0xF2, 0xFD), // Light blue
            new SKColor(0xF3, 0xE5, 0xF5), // Light purple
            new SKColor(0xFF, 0xF3, 0xE0), // Light orange
            new SKColor(0xF1, 0xF8, 0xE9), // Light lime
            new SKColor(0xE0, 0xF2, 0xF1), // Light teal
            new SKColor(0xF9, 0xF9, 0xF9), // Light gray
            new SKColor(0xFF, 0xEB, 0xEE)  // Light pink
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunityDetector"/> class.
        /// </summary>
        public CommunityDetector()
        {
            Width = 300;
            Height = 180;
            Name = "CommunityDetector";
            DisplayText = "Communities";
            TextPosition = TextPosition.Above;
            PrimaryColor = new SKColor(0x9C, 0x27, 0xB0); // Purple
        }

        /// <summary>
        /// Detects communities in the given network.
        /// </summary>
        /// <param name="nodes">The network nodes.</param>
        /// <param name="links">The network links.</param>
        public void DetectCommunities(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            Communities.Clear();

            if (nodes == null || nodes.Count == 0)
                return;

            switch (Algorithm)
            {
                case CommunityAlgorithm.Louvain:
                    DetectCommunitiesLouvain(nodes, links);
                    break;
                case CommunityAlgorithm.GirvanNewman:
                    DetectCommunitiesGirvanNewman(nodes, links);
                    break;
                case CommunityAlgorithm.LabelPropagation:
                    DetectCommunitiesLabelPropagation(nodes, links);
                    break;
                case CommunityAlgorithm.ConnectedComponents:
                    DetectCommunitiesConnectedComponents(nodes, links);
                    break;
            }

            // Filter small communities
            Communities.RemoveAll(c => c.Nodes.Count < MinCommunitySize);

            // Assign colors and apply visual styling
            AssignCommunityColors();
            ApplyCommunityStyling();
        }

        /// <summary>
        /// Detects communities using the Louvain method (simplified).
        /// </summary>
        private void DetectCommunitiesLouvain(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            // Simplified Louvain method - in practice this would be much more complex
            // For now, we'll use a basic clustering approach
            var visited = new HashSet<NetworkNode>();
            var adjacencyList = BuildAdjacencyList(nodes, links);

            foreach (var node in nodes)
            {
                if (visited.Contains(node))
                    continue;

                var community = new Community { Id = Communities.Count + 1 };
                var queue = new Queue<NetworkNode>();
                queue.Enqueue(node);
                visited.Add(node);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    community.Nodes.Add(current);

                    foreach (var neighbor in adjacencyList[current])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                Communities.Add(community);
            }
        }

        /// <summary>
        /// Detects communities using Girvan-Newman algorithm (simplified).
        /// </summary>
        private void DetectCommunitiesGirvanNewman(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            // Simplified Girvan-Newman - remove edges with high betweenness
            var remainingLinks = new List<NetworkLink>(links);
            var communities = new List<List<NetworkNode>>();

            // Start with each node in its own community
            foreach (var node in nodes)
            {
                communities.Add(new List<NetworkNode> { node });
            }

            // Simplified: merge communities based on edge density
            while (remainingLinks.Count > 0 && communities.Count > 1)
            {
                // Find the edge with highest betweenness (simplified)
                var edgeToRemove = remainingLinks.OrderByDescending(l =>
                    CalculateEdgeBetweenness(l, nodes, remainingLinks)).First();

                remainingLinks.Remove(edgeToRemove);

                // Merge communities connected by remaining edges
                MergeCommunitiesBasedOnConnectivity(communities, remainingLinks, nodes);
            }

            // Create Community objects
            foreach (var communityNodes in communities)
            {
                Communities.Add(new Community
                {
                    Id = Communities.Count + 1,
                    Nodes = new List<NetworkNode>(communityNodes)
                });
            }
        }

        /// <summary>
        /// Detects communities using label propagation algorithm.
        /// </summary>
        private void DetectCommunitiesLabelPropagation(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var adjacencyList = BuildAdjacencyList(nodes, links);
            var labels = new Dictionary<NetworkNode, int>();
            var random = new Random();

            // Initialize each node with its own label
            for (int i = 0; i < nodes.Count; i++)
            {
                labels[nodes[i]] = i;
            }

            // Label propagation iterations
            bool changed = true;
            int maxIterations = 10;

            for (int iteration = 0; iteration < maxIterations && changed; iteration++)
            {
                changed = false;
                var newLabels = new Dictionary<NetworkNode, int>(labels);

                // Shuffle nodes for random order
                var shuffledNodes = nodes.OrderBy(n => random.Next()).ToList();

                foreach (var node in shuffledNodes)
                {
                    if (adjacencyList[node].Count == 0)
                        continue;

                    // Count label frequencies among neighbors
                    var labelCounts = new Dictionary<int, int>();
                    foreach (var neighbor in adjacencyList[node])
                    {
                        int neighborLabel = labels[neighbor];
                        labelCounts[neighborLabel] = labelCounts.GetValueOrDefault(neighborLabel, 0) + 1;
                    }

                    // Choose the most frequent label (break ties randomly)
                    int maxCount = labelCounts.Values.Max();
                    var candidateLabels = labelCounts.Where(kvp => kvp.Value == maxCount).Select(kvp => kvp.Key).ToList();
                    int newLabel = candidateLabels[random.Next(candidateLabels.Count)];

                    if (newLabels[node] != newLabel)
                    {
                        newLabels[node] = newLabel;
                        changed = true;
                    }
                }

                labels = newLabels;
            }

            // Group nodes by label
            var labelGroups = labels.GroupBy(kvp => kvp.Value);
            foreach (var group in labelGroups)
            {
                Communities.Add(new Community
                {
                    Id = Communities.Count + 1,
                    Nodes = group.Select(kvp => kvp.Key).ToList()
                });
            }
        }

        /// <summary>
        /// Detects communities using connected components algorithm.
        /// </summary>
        private void DetectCommunitiesConnectedComponents(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var visited = new HashSet<NetworkNode>();
            var adjacencyList = BuildAdjacencyList(nodes, links);

            foreach (var node in nodes)
            {
                if (visited.Contains(node))
                    continue;

                var community = new Community { Id = Communities.Count + 1 };
                var stack = new Stack<NetworkNode>();
                stack.Push(node);
                visited.Add(node);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    community.Nodes.Add(current);

                    foreach (var neighbor in adjacencyList[current])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            stack.Push(neighbor);
                        }
                    }
                }

                Communities.Add(community);
            }
        }

        /// <summary>
        /// Builds adjacency list from nodes and links.
        /// </summary>
        private Dictionary<NetworkNode, List<NetworkNode>> BuildAdjacencyList(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var adjacencyList = new Dictionary<NetworkNode, List<NetworkNode>>();

            foreach (var node in nodes)
            {
                adjacencyList[node] = new List<NetworkNode>();
            }

            foreach (var link in links)
            {
                adjacencyList[link.SourceNode].Add(link.TargetNode);
                adjacencyList[link.TargetNode].Add(link.SourceNode); // Undirected
            }

            return adjacencyList;
        }

        /// <summary>
        /// Calculates edge betweenness centrality (simplified).
        /// </summary>
        private double CalculateEdgeBetweenness(NetworkLink link, List<NetworkNode> nodes, List<NetworkLink> links)
        {
            // Simplified betweenness calculation
            // In practice, this would use more sophisticated algorithms
            return 1.0; // Placeholder
        }

        /// <summary>
        /// Merges communities based on connectivity.
        /// </summary>
        private void MergeCommunitiesBasedOnConnectivity(List<List<NetworkNode>> communities, List<NetworkLink> links, List<NetworkNode> nodes)
        {
            // Simplified merging logic
            var communityMap = new Dictionary<NetworkNode, int>();
            for (int i = 0; i < communities.Count; i++)
            {
                foreach (var node in communities[i])
                {
                    communityMap[node] = i;
                }
            }

            // This is a placeholder for more sophisticated merging
        }

        /// <summary>
        /// Assigns colors to communities.
        /// </summary>
        private void AssignCommunityColors()
        {
            for (int i = 0; i < Communities.Count; i++)
            {
                Communities[i].Color = CommunityColors[i % CommunityColors.Count];
            }
        }

        /// <summary>
        /// Applies visual styling to nodes based on their community membership.
        /// </summary>
        private void ApplyCommunityStyling()
        {
            foreach (var community in Communities)
            {
                foreach (var node in community.Nodes)
                {
                    node.CommunityColor = community.Color;
                    node.CommunityId = community.Id;
                }
            }
        }

        /// <summary>
        /// Draws the community detector control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, SKColors.White);

            // Draw title
            float currentY = Y + 15;
            float lineHeight = 16;
            float leftMargin = X + 10;

            using var titleFont = new SKFont { Size = 14 };
            using var titlePaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
            canvas.DrawText($"{Algorithm} Communities", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, titleFont, titlePaint);

            currentY += lineHeight + 5;

            // Draw community information
            using var font = new SKFont { Size = 11 };
            using var namePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var countPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };

            int displayCount = Math.Min(8, Communities.Count); // Limit display
            for (int i = 0; i < displayCount; i++)
            {
                var community = Communities[i];

                // Draw color indicator
                var colorRect = new SKRect(leftMargin, currentY - 2, leftMargin + 12, currentY + 10);
                DrawFilledRect(canvas, colorRect, community.Color);

                // Draw community info
                string communityText = ShowLabels ? $"Community {community.Id}" : $"Group {community.Id}";
                string countText = $"{community.Nodes.Count} nodes";

                canvas.DrawText(communityText, leftMargin + 16, currentY + lineHeight - 3, SKTextAlign.Left, font, namePaint);
                canvas.DrawText(countText, X + Width - 10, currentY + lineHeight - 3, SKTextAlign.Right, font, countPaint);

                currentY += lineHeight;
            }

            // Draw summary
            if (Communities.Count > displayCount)
            {
                string summaryText = $"+{Communities.Count - displayCount} more communities";
                canvas.DrawText(summaryText, leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, countPaint);
            }
        }
    }

    /// <summary>
    /// Represents a network community.
    /// </summary>
    public class Community
    {
        /// <summary>
        /// Gets or sets the community ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the list of nodes in this community.
        /// </summary>
        public List<NetworkNode> Nodes { get; set; } = new List<NetworkNode>();

        /// <summary>
        /// Gets or sets the color assigned to this community.
        /// </summary>
        public SKColor Color { get; set; }

        /// <summary>
        /// Gets the modularity score for this community.
        /// </summary>
        public double Modularity { get; set; }
    }

    /// <summary>
    /// Community detection algorithms.
    /// </summary>
    public enum CommunityAlgorithm
    {
        /// <summary>
        /// Louvain method for community detection.
        /// </summary>
        Louvain,

        /// <summary>
        /// Girvan-Newman algorithm.
        /// </summary>
        GirvanNewman,

        /// <summary>
        /// Label propagation algorithm.
        /// </summary>
        LabelPropagation,

        /// <summary>
        /// Connected components algorithm.
        /// </summary>
        ConnectedComponents
    }
}