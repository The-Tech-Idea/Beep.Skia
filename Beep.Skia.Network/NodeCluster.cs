using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Represents a cluster/grouping of related network nodes.
    /// Provides visual grouping with background and border.
    /// </summary>
    public class NodeCluster : NetworkControl
    {
        /// <summary>
        /// Gets the collection of nodes in this cluster.
        /// </summary>
        public List<NetworkNode> Nodes { get; } = new List<NetworkNode>();

        /// <summary>
        /// Gets or sets the cluster name/label.
        /// </summary>
        public string ClusterName { get; set; } = "Cluster";

        /// <summary>
        /// Gets or sets the background color for the cluster.
        /// </summary>
        public SKColor ClusterBackground { get; set; } = new SKColor(0xE3, 0xF2, 0xFD, 0x80); // Light blue with transparency

        /// <summary>
        /// Gets or sets the border color for the cluster.
        /// </summary>
        public SKColor ClusterBorder { get; set; } = new SKColor(0x42, 0xA5, 0xF5, 0xC0); // Blue with transparency

        /// <summary>
        /// Gets or sets the padding around the cluster bounds.
        /// </summary>
        public float Padding { get; set; } = 20f;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeCluster"/> class.
        /// </summary>
        public NodeCluster()
        {
            Width = 200;
            Height = 150;
            Name = "NodeCluster";
            DisplayText = ClusterName;
            TextPosition = TextPosition.Above;
            PrimaryColor = new SKColor(0x42, 0xA5, 0xF5); // Blue
        }

        /// <summary>
        /// Adds a node to this cluster.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public void AddNode(NetworkNode node)
        {
            if (!Nodes.Contains(node))
            {
                Nodes.Add(node);
                UpdateBounds();
            }
        }

        /// <summary>
        /// Removes a node from this cluster.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        public void RemoveNode(NetworkNode node)
        {
            if (Nodes.Remove(node))
            {
                UpdateBounds();
            }
        }

        /// <summary>
        /// Updates the cluster bounds based on contained nodes.
        /// </summary>
        protected override void UpdateBounds()
        {
            if (Nodes.Count == 0)
                return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var node in Nodes)
            {
                minX = Math.Min(minX, node.X);
                minY = Math.Min(minY, node.Y);
                maxX = Math.Max(maxX, node.X + node.Width);
                maxY = Math.Max(maxY, node.Y + node.Height);
            }

            X = minX - Padding;
            Y = minY - Padding;
            Width = (maxX - minX) + (Padding * 2);
            Height = (maxY - minY) + (Padding * 2);
        }

        /// <summary>
        /// Draws the cluster background and border.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var clusterRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw cluster background
            using var bgPaint = new SKPaint
            {
                Color = GetEffectiveFillColor(ClusterBackground),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(clusterRect, CornerRadius * 2, CornerRadius * 2, bgPaint);

            // Draw cluster border
            using var borderPaint = new SKPaint
            {
                Color = ClusterBorder,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = BorderThickness * 1.5f,
                IsAntialias = true
            };
            canvas.DrawRoundRect(clusterRect, CornerRadius * 2, CornerRadius * 2, borderPaint);

            // Draw cluster name
            if (!string.IsNullOrEmpty(ClusterName))
            {
                DrawCenteredText(canvas, ClusterName, clusterRect, TextFontSize, TextColor);
            }
        }
    }
}