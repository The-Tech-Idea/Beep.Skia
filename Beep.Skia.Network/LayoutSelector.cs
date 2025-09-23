using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Layout selector component that provides controls for choosing and applying different graph layout algorithms.
    /// Supports force-directed, circular, hierarchical, and other layout types.
    /// </summary>
    public class LayoutSelector : NetworkControl
    {
        /// <summary>
        /// Gets or sets the currently selected layout algorithm.
        /// </summary>
        public LayoutAlgorithm SelectedAlgorithm { get; set; } = LayoutAlgorithm.ForceDirected;

        /// <summary>
        /// Gets the list of available layout algorithms.
        /// </summary>
        public List<LayoutAlgorithm> AvailableAlgorithms { get; } = new List<LayoutAlgorithm>
        {
            LayoutAlgorithm.ForceDirected,
            LayoutAlgorithm.Circular,
            LayoutAlgorithm.Hierarchical,
            LayoutAlgorithm.Grid,
            LayoutAlgorithm.Random,
            LayoutAlgorithm.Radial
        };

        /// <summary>
        /// Gets or sets the layout parameters.
        /// </summary>
        public LayoutParameters Parameters { get; set; } = new LayoutParameters();

        /// <summary>
        /// Gets or sets whether the layout is currently being calculated.
        /// </summary>
        public bool IsCalculating { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show layout options.
        /// </summary>
        public bool ShowOptions { get; set; } = true;

        /// <summary>
        /// Event raised when the layout algorithm changes.
        /// </summary>
        public event EventHandler<LayoutChangedEventArgs> LayoutChanged;

        /// <summary>
        /// Event raised when layout parameters change.
        /// </summary>
        public event EventHandler<LayoutParametersChangedEventArgs> ParametersChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutSelector"/> class.
        /// </summary>
        public LayoutSelector()
        {
            Width = 250;
            Height = 180;
            Name = "LayoutSelector";
            DisplayText = "Layout";
            TextPosition = TextPosition.Above;
            PrimaryColor = new SKColor(0x4C, 0xAF, 0x50); // Green
        }

        /// <summary>
        /// Sets the layout algorithm.
        /// </summary>
        /// <param name="algorithm">The layout algorithm to use.</param>
        public void SetAlgorithm(LayoutAlgorithm algorithm)
        {
            if (SelectedAlgorithm != algorithm)
            {
                SelectedAlgorithm = algorithm;
                OnLayoutChanged();
            }
        }

        /// <summary>
        /// Applies the current layout to the given nodes.
        /// </summary>
        /// <param name="nodes">The nodes to layout.</param>
        /// <param name="links">The links between nodes.</param>
        /// <param name="bounds">The bounds to layout within.</param>
        public void ApplyLayout(List<NetworkNode> nodes, List<NetworkLink> links, SKRect bounds)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            IsCalculating = true;

            try
            {
                switch (SelectedAlgorithm)
                {
                    case LayoutAlgorithm.ForceDirected:
                        ApplyForceDirectedLayout(nodes, links, bounds);
                        break;
                    case LayoutAlgorithm.Circular:
                        ApplyCircularLayout(nodes, bounds);
                        break;
                    case LayoutAlgorithm.Hierarchical:
                        ApplyHierarchicalLayout(nodes, links, bounds);
                        break;
                    case LayoutAlgorithm.Grid:
                        ApplyGridLayout(nodes, bounds);
                        break;
                    case LayoutAlgorithm.Random:
                        ApplyRandomLayout(nodes, bounds);
                        break;
                    case LayoutAlgorithm.Radial:
                        ApplyRadialLayout(nodes, bounds);
                        break;
                }
            }
            finally
            {
                IsCalculating = false;
            }
        }

        /// <summary>
        /// Applies force-directed layout algorithm.
        /// </summary>
        private void ApplyForceDirectedLayout(List<NetworkNode> nodes, List<NetworkLink> links, SKRect bounds)
        {
            // Simplified force-directed layout
            // In a real implementation, this would use iterative physics simulation
            float centerX = bounds.MidX;
            float centerY = bounds.MidY;
            float radius = Math.Min(bounds.Width, bounds.Height) / 3;

            for (int i = 0; i < nodes.Count; i++)
            {
                double angle = (2 * Math.PI * i) / nodes.Count;
                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle));

                nodes[i].X = x;
                nodes[i].Y = y;
            }
        }

        /// <summary>
        /// Applies circular layout algorithm.
        /// </summary>
        private void ApplyCircularLayout(List<NetworkNode> nodes, SKRect bounds)
        {
            float centerX = bounds.MidX;
            float centerY = bounds.MidY;
            float radius = Math.Min(bounds.Width, bounds.Height) / 3;

            for (int i = 0; i < nodes.Count; i++)
            {
                double angle = (2 * Math.PI * i) / nodes.Count;
                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle));

                nodes[i].X = x;
                nodes[i].Y = y;
            }
        }

        /// <summary>
        /// Applies hierarchical layout algorithm.
        /// </summary>
        private void ApplyHierarchicalLayout(List<NetworkNode> nodes, List<NetworkLink> links, SKRect bounds)
        {
            // Simplified hierarchical layout
            // Assign levels based on connectivity (simplified)
            var levels = AssignHierarchicalLevels(nodes, links);

            float levelHeight = bounds.Height / (levels.Keys.Count + 1);
            float currentY = bounds.Top + levelHeight;

            foreach (var level in levels.OrderBy(kvp => kvp.Key))
            {
                float levelWidth = bounds.Width / (level.Value.Count + 1);
                float currentX = bounds.Left + levelWidth;

                foreach (var node in level.Value)
                {
                    node.X = currentX;
                    node.Y = currentY;
                    currentX += levelWidth;
                }

                currentY += levelHeight;
            }
        }

        /// <summary>
        /// Applies grid layout algorithm.
        /// </summary>
        private void ApplyGridLayout(List<NetworkNode> nodes, SKRect bounds)
        {
            int cols = (int)Math.Ceiling(Math.Sqrt(nodes.Count));
            int rows = (int)Math.Ceiling((double)nodes.Count / cols);

            float cellWidth = bounds.Width / cols;
            float cellHeight = bounds.Height / rows;

            for (int i = 0; i < nodes.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                nodes[i].X = bounds.Left + col * cellWidth + cellWidth / 2;
                nodes[i].Y = bounds.Top + row * cellHeight + cellHeight / 2;
            }
        }

        /// <summary>
        /// Applies random layout algorithm.
        /// </summary>
        private void ApplyRandomLayout(List<NetworkNode> nodes, SKRect bounds)
        {
            var random = new Random();
            float padding = 20;

            foreach (var node in nodes)
            {
                node.X = bounds.Left + padding + (float)random.NextDouble() * (bounds.Width - 2 * padding);
                node.Y = bounds.Top + padding + (float)random.NextDouble() * (bounds.Height - 2 * padding);
            }
        }

        /// <summary>
        /// Applies radial layout algorithm.
        /// </summary>
        private void ApplyRadialLayout(List<NetworkNode> nodes, SKRect bounds)
        {
            // Similar to circular but with different radius distribution
            float centerX = bounds.MidX;
            float centerY = bounds.MidY;
            float maxRadius = Math.Min(bounds.Width, bounds.Height) / 3;

            for (int i = 0; i < nodes.Count; i++)
            {
                double angle = (2 * Math.PI * i) / nodes.Count;
                // Vary radius based on node index for more interesting layout
                float radius = maxRadius * (0.3f + 0.7f * (i / (float)nodes.Count));

                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle));

                nodes[i].X = x;
                nodes[i].Y = y;
            }
        }

        /// <summary>
        /// Assigns hierarchical levels to nodes (simplified).
        /// </summary>
        private Dictionary<int, List<NetworkNode>> AssignHierarchicalLevels(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var levels = new Dictionary<int, List<NetworkNode>>();
            var processed = new HashSet<NetworkNode>();

            // Start with nodes that have no incoming links (roots)
            var roots = nodes.Where(n => !links.Any(l => l.TargetNode == n)).ToList();

            int currentLevel = 0;
            var currentLevelNodes = new List<NetworkNode>(roots);

            while (currentLevelNodes.Count > 0)
            {
                levels[currentLevel] = currentLevelNodes;
                var nextLevelNodes = new List<NetworkNode>();

                foreach (var node in currentLevelNodes)
                {
                    processed.Add(node);

                    // Add nodes that this node connects to
                    var connectedNodes = links.Where(l => l.SourceNode == node && !processed.Contains(l.TargetNode))
                                            .Select(l => l.TargetNode)
                                            .Distinct()
                                            .ToList();

                    nextLevelNodes.AddRange(connectedNodes);
                }

                currentLevelNodes = nextLevelNodes.Distinct().ToList();
                currentLevel++;
            }

            // Handle any remaining nodes (cycles or disconnected)
            foreach (var node in nodes.Where(n => !processed.Contains(n)))
            {
                if (!levels.ContainsKey(currentLevel))
                    levels[currentLevel] = new List<NetworkNode>();
                levels[currentLevel].Add(node);
            }

            return levels;
        }

        /// <summary>
        /// Handles mouse down events for layout selection.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (!Bounds.Contains(point))
                return false;

            // Handle algorithm selection clicks
            float currentY = Y + 35;
            float buttonHeight = 18;
            float leftMargin = X + 10;

            for (int i = 0; i < AvailableAlgorithms.Count; i++)
            {
                var buttonRect = new SKRect(leftMargin, currentY, X + Width - 10, currentY + buttonHeight);

                if (buttonRect.Contains(point))
                {
                    SetAlgorithm(AvailableAlgorithms[i]);
                    return true;
                }

                currentY += buttonHeight + 2;
            }

            return false;
        }

        /// <summary>
        /// Draws the layout selector control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, SKColors.White);

            if (!ShowOptions)
                return;

            float currentY = Y + 15;
            float lineHeight = 16;
            float leftMargin = X + 10;

            // Draw title
            using var titleFont = new SKFont { Size = 12 };
            using var titlePaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
            canvas.DrawText("Graph Layout", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, titleFont, titlePaint);

            currentY += lineHeight + 5;

            // Draw algorithm buttons
            using var buttonFont = new SKFont { Size = 10 };
            using var buttonPaint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true };
            using var selectedPaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

            float buttonHeight = 18;

            foreach (var algorithm in AvailableAlgorithms)
            {
                var buttonRect = new SKRect(leftMargin, currentY, X + Width - 10, currentY + buttonHeight);

                // Highlight selected algorithm
                var paint = algorithm == SelectedAlgorithm ? PrimaryColor : SKColors.LightGray;
                DrawFilledRect(canvas, buttonRect, paint);

                // Draw algorithm name
                string algorithmName = GetAlgorithmDisplayName(algorithm);
                canvas.DrawText(algorithmName, buttonRect.MidX, buttonRect.MidY + 3, SKTextAlign.Center, buttonFont, textPaint);

                currentY += buttonHeight + 2;
            }

            // Draw status
            if (IsCalculating)
            {
                using var statusFont = new SKFont { Size = 9 };
                using var statusPaint = new SKPaint { Color = SKColors.Orange, IsAntialias = true };
                canvas.DrawText("Calculating...", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, statusFont, statusPaint);
            }
        }

        /// <summary>
        /// Gets the display name for a layout algorithm.
        /// </summary>
        private string GetAlgorithmDisplayName(LayoutAlgorithm algorithm)
        {
            return algorithm switch
            {
                LayoutAlgorithm.ForceDirected => "Force Directed",
                LayoutAlgorithm.Circular => "Circular",
                LayoutAlgorithm.Hierarchical => "Hierarchical",
                LayoutAlgorithm.Grid => "Grid",
                LayoutAlgorithm.Random => "Random",
                LayoutAlgorithm.Radial => "Radial",
                _ => algorithm.ToString()
            };
        }

        /// <summary>
        /// Raises the LayoutChanged event.
        /// </summary>
        protected virtual void OnLayoutChanged()
        {
            LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(SelectedAlgorithm));
        }

        /// <summary>
        /// Raises the ParametersChanged event.
        /// </summary>
        protected virtual void OnParametersChanged()
        {
            ParametersChanged?.Invoke(this, new LayoutParametersChangedEventArgs(Parameters));
        }
    }

    /// <summary>
    /// Layout algorithm types.
    /// </summary>
    public enum LayoutAlgorithm
    {
        /// <summary>
        /// Force-directed layout algorithm.
        /// </summary>
        ForceDirected,

        /// <summary>
        /// Circular layout algorithm.
        /// </summary>
        Circular,

        /// <summary>
        /// Hierarchical layout algorithm.
        /// </summary>
        Hierarchical,

        /// <summary>
        /// Grid layout algorithm.
        /// </summary>
        Grid,

        /// <summary>
        /// Random layout algorithm.
        /// </summary>
        Random,

        /// <summary>
        /// Radial layout algorithm.
        /// </summary>
        Radial
    }

    /// <summary>
    /// Parameters for layout algorithms.
    /// </summary>
    public class LayoutParameters
    {
        /// <summary>
        /// Gets or sets the attraction force strength.
        /// </summary>
        public float AttractionStrength { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the repulsion force strength.
        /// </summary>
        public float RepulsionStrength { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the damping factor.
        /// </summary>
        public float Damping { get; set; } = 0.9f;

        /// <summary>
        /// Gets or sets the maximum iterations for iterative algorithms.
        /// </summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>
        /// Gets or sets the convergence threshold.
        /// </summary>
        public float ConvergenceThreshold { get; set; } = 0.01f;
    }

    /// <summary>
    /// Event arguments for layout change events.
    /// </summary>
    public class LayoutChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the selected layout algorithm.
        /// </summary>
        public LayoutAlgorithm Algorithm { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutChangedEventArgs"/> class.
        /// </summary>
        /// <param name="algorithm">The selected algorithm.</param>
        public LayoutChangedEventArgs(LayoutAlgorithm algorithm)
        {
            Algorithm = algorithm;
        }
    }

    /// <summary>
    /// Event arguments for layout parameters change events.
    /// </summary>
    public class LayoutParametersChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the layout parameters.
        /// </summary>
        public LayoutParameters Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutParametersChangedEventArgs"/> class.
        /// </summary>
        /// <param name="parameters">The layout parameters.</param>
        public LayoutParametersChangedEventArgs(LayoutParameters parameters)
        {
            Parameters = parameters;
        }
    }
}