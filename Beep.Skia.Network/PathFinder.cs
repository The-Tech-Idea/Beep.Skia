using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Path finder component that visualizes shortest paths between network nodes.
    /// Highlights the path and displays path metrics.
    /// </summary>
    public class PathFinder : NetworkControl
    {
        /// <summary>
        /// Gets or sets the start node for path finding.
        /// </summary>
        public NetworkNode StartNode { get; set; }

        /// <summary>
        /// Gets or sets the end node for path finding.
        /// </summary>
        public NetworkNode EndNode { get; set; }

        /// <summary>
        /// Gets the list of nodes in the current path.
        /// </summary>
        public List<NetworkNode> PathNodes { get; } = new List<NetworkNode>();

        /// <summary>
        /// Gets the list of links in the current path.
        /// </summary>
        public List<NetworkLink> PathLinks { get; } = new List<NetworkLink>();

        /// <summary>
        /// Gets or sets the path length (number of edges).
        /// </summary>
        public int PathLength { get; set; } = 0;

        /// <summary>
        /// Gets or sets the path cost/distance.
        /// </summary>
        public double PathCost { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the color for highlighting the path.
        /// </summary>
    public SKColor PathHighlightColor { get; set; } = MaterialColors.Tertiary;

        /// <summary>
        /// Gets or sets the color for highlighting path nodes.
        /// </summary>
    public SKColor NodeHighlightColor { get; set; } = MaterialColors.Secondary;

        /// <summary>
        /// Gets or sets whether to show path metrics.
        /// </summary>
        public bool ShowMetrics { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFinder"/> class.
        /// </summary>
        public PathFinder()
        {
            Width = 250;
            Height = 120;
            Name = "PathFinder";
            DisplayText = "Shortest Path";
            TextPosition = TextPosition.Above;
            PrimaryColor = MaterialColors.Tertiary;
        }

        /// <summary>
        /// Sets the path to visualize.
        /// </summary>
        /// <param name="nodes">The nodes in the path.</param>
        /// <param name="links">The links in the path.</param>
        /// <param name="cost">The path cost.</param>
        public void SetPath(List<NetworkNode> nodes, List<NetworkLink> links, double cost = 0.0)
        {
            PathNodes.Clear();
            PathLinks.Clear();

            if (nodes != null)
                PathNodes.AddRange(nodes);
            if (links != null)
                PathLinks.AddRange(links);

            PathLength = PathLinks.Count;
            PathCost = cost;

            StartNode = PathNodes.Count > 0 ? PathNodes[0] : null;
            EndNode = PathNodes.Count > 1 ? PathNodes[PathNodes.Count - 1] : null;
        }

        /// <summary>
        /// Clears the current path.
        /// </summary>
        public void ClearPath()
        {
            PathNodes.Clear();
            PathLinks.Clear();
            PathLength = 0;
            PathCost = 0.0;
            StartNode = null;
            EndNode = null;
        }

        /// <summary>
        /// Draws the path finder control and highlights the path.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, MaterialColors.SurfaceContainer);

            // Draw path information
            float currentY = Y + 15;
            float lineHeight = 16;
            float leftMargin = X + 10;

            using var font = new SKFont { Size = 12 };
            using var labelPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var valuePaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };

            if (StartNode != null && EndNode != null)
            {
                canvas.DrawText($"From: {StartNode.Name}", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, labelPaint);
                currentY += lineHeight;
                canvas.DrawText($"To: {EndNode.Name}", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, labelPaint);
                currentY += lineHeight;

                if (ShowMetrics)
                {
                    canvas.DrawText($"Length: {PathLength}", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, valuePaint);
                    currentY += lineHeight;
                    canvas.DrawText($"Cost: {PathCost:F2}", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, valuePaint);
                }
            }
            else
            {
                canvas.DrawText("No path selected", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, font, labelPaint);
            }

            // Highlight the path in the network (this would be handled by the parent graph)
            HighlightPathInNetwork();
        }

        /// <summary>
        /// Highlights the path nodes and links in the network visualization.
        /// This method should be called by the parent graph component.
        /// </summary>
        public void HighlightPathInNetwork()
        {
            // Mark path nodes for highlighting
            foreach (var node in PathNodes)
            {
                node.IsHighlighted = true;
            }

            // Mark path links for highlighting
            foreach (var link in PathLinks)
            {
                link.IsHighlighted = true;
            }
        }

        /// <summary>
        /// Clears path highlighting from the network.
        /// </summary>
        public void ClearPathHighlighting()
        {
            foreach (var node in PathNodes)
            {
                node.IsHighlighted = false;
            }

            foreach (var link in PathLinks)
            {
                link.IsHighlighted = false;
            }
        }
    }
}