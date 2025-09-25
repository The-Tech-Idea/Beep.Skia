using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Graph navigator component that provides interactive controls for exploring network graphs.
    /// Includes zoom, pan, node selection, and navigation history.
    /// </summary>
    public class GraphNavigator : NetworkControl
    {
        /// <summary>
        /// Gets or sets the current zoom level (1.0 = 100%).
        /// </summary>
        public float ZoomLevel { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the minimum zoom level.
        /// </summary>
        public float MinZoom { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the maximum zoom level.
        /// </summary>
        public float MaxZoom { get; set; } = 5.0f;

        /// <summary>
        /// Gets or sets the pan offset X.
        /// </summary>
        public float PanOffsetX { get; set; } = 0;

        /// <summary>
        /// Gets or sets the pan offset Y.
        /// </summary>
        public float PanOffsetY { get; set; } = 0;

        /// <summary>
        /// Gets the currently selected node.
        /// </summary>
        public NetworkNode SelectedNode { get; private set; }

        /// <summary>
        /// Gets the list of navigation history (node selections).
        /// </summary>
        public List<NetworkNode> NavigationHistory { get; } = new List<NetworkNode>();

        /// <summary>
        /// Gets or sets whether the navigator is in pan mode.
        /// </summary>
        public bool IsPanMode { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show navigation controls.
        /// </summary>
        public bool ShowControls { get; set; } = true;

        /// <summary>
        /// Gets or sets the zoom step size.
        /// </summary>
        public float ZoomStep { get; set; } = 0.25f;

        /// <summary>
        /// Event raised when the selected node changes.
        /// </summary>
        public event EventHandler<NodeSelectionEventArgs> NodeSelected;

        /// <summary>
        /// Event raised when the view changes (zoom/pan).
        /// </summary>
        public event EventHandler<ViewChangedEventArgs> ViewChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNavigator"/> class.
        /// </summary>
        public GraphNavigator()
        {
            Width = 200;
            Height = 150;
            Name = "GraphNavigator";
            DisplayText = "Navigator";
            TextPosition = TextPosition.Above;
            PrimaryColor = MaterialColors.Primary;
        }

        /// <summary>
        /// Zooms in by one step.
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(ZoomLevel + ZoomStep);
        }

        /// <summary>
        /// Zooms out by one step.
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(ZoomLevel - ZoomStep);
        }

        /// <summary>
        /// Sets the zoom level.
        /// </summary>
        /// <param name="zoom">The new zoom level.</param>
        public void SetZoom(float zoom)
        {
            ZoomLevel = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
            OnViewChanged();
        }

        /// <summary>
        /// Resets the view to default zoom and pan.
        /// </summary>
        public void ResetView()
        {
            ZoomLevel = 1.0f;
            PanOffsetX = 0;
            PanOffsetY = 0;
            OnViewChanged();
        }

        /// <summary>
        /// Pans the view by the specified amount.
        /// </summary>
        /// <param name="deltaX">The change in X position.</param>
        /// <param name="deltaY">The change in Y position.</param>
        public void Pan(float deltaX, float deltaY)
        {
            PanOffsetX += deltaX;
            PanOffsetY += deltaY;
            OnViewChanged();
        }

        /// <summary>
        /// Centers the view on a specific node.
        /// </summary>
        /// <param name="node">The node to center on.</param>
        public void CenterOnNode(NetworkNode node)
        {
            if (node != null)
            {
                // This would typically center the viewport on the node
                // Implementation depends on the parent graph component
                SelectNode(node);
            }
        }

        /// <summary>
        /// Selects a node and adds it to navigation history.
        /// </summary>
        /// <param name="node">The node to select.</param>
        public void SelectNode(NetworkNode node)
        {
            if (SelectedNode != node)
            {
                SelectedNode = node;

                if (node != null)
                {
                    NavigationHistory.Add(node);
                    // Keep only last 10 selections
                    if (NavigationHistory.Count > 10)
                    {
                        NavigationHistory.RemoveAt(0);
                    }
                }

                OnNodeSelected(node);
            }
        }

        /// <summary>
        /// Goes back to the previously selected node.
        /// </summary>
        public void GoBack()
        {
            if (NavigationHistory.Count > 1)
            {
                NavigationHistory.RemoveAt(NavigationHistory.Count - 1);
                var previousNode = NavigationHistory.Last();
                SelectNode(previousNode);
            }
        }

        /// <summary>
        /// Clears the navigation history.
        /// </summary>
        public void ClearHistory()
        {
            NavigationHistory.Clear();
            SelectedNode = null;
        }

        /// <summary>
        /// Handles mouse down events for navigation.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (!Bounds.Contains(point))
                return false;

            // Handle control clicks
            var zoomInRect = new SKRect(X + 10, Y + 30, X + 40, Y + 50);
            var zoomOutRect = new SKRect(X + 50, Y + 30, X + 80, Y + 50);
            var resetRect = new SKRect(X + 90, Y + 30, X + 140, Y + 50);

            if (zoomInRect.Contains(point))
            {
                ZoomIn();
                return true;
            }
            else if (zoomOutRect.Contains(point))
            {
                ZoomOut();
                return true;
            }
            else if (resetRect.Contains(point))
            {
                ResetView();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Draws the graph navigator control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, MaterialColors.SurfaceContainer);

            if (!ShowControls)
                return;

            float currentY = Y + 15;
            float lineHeight = 16;
            float leftMargin = X + 10;

            // Draw title
            using var titleFont = new SKFont { Size = 12 };
            using var titlePaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
            canvas.DrawText("Navigation", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, titleFont, titlePaint);

            currentY += lineHeight + 5;

            // Draw zoom controls
            using var controlFont = new SKFont { Size = 10 };
            using var controlPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var buttonPaint = new SKPaint { Color = MaterialColors.SurfaceVariant, IsAntialias = true };

            // Zoom buttons
            var zoomInRect = new SKRect(leftMargin, currentY, leftMargin + 25, currentY + 18);
            var zoomOutRect = new SKRect(leftMargin + 30, currentY, leftMargin + 55, currentY + 18);
            var resetRect = new SKRect(leftMargin + 60, currentY, leftMargin + 110, currentY + 18);

            DrawFilledRect(canvas, zoomInRect, MaterialColors.SurfaceVariant);
            DrawFilledRect(canvas, zoomOutRect, MaterialColors.SurfaceVariant);
            DrawFilledRect(canvas, resetRect, MaterialColors.SurfaceVariant);

            canvas.DrawText("+", zoomInRect.MidX, zoomInRect.MidY + 3, SKTextAlign.Center, controlFont, controlPaint);
            canvas.DrawText("-", zoomOutRect.MidX, zoomOutRect.MidY + 3, SKTextAlign.Center, controlFont, controlPaint);
            canvas.DrawText("Reset", resetRect.MidX, resetRect.MidY + 3, SKTextAlign.Center, controlFont, controlPaint);

            currentY += 25;

            // Draw zoom level
            using var infoFont = new SKFont { Size = 10 };
            using var infoPaint = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true };
            string zoomText = $"Zoom: {(ZoomLevel * 100):F0}%";
            canvas.DrawText(zoomText, leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, infoFont, infoPaint);

            currentY += lineHeight;

            // Draw selected node info
            if (SelectedNode != null)
            {
                string nodeText = $"Selected: {SelectedNode.Name}";
                canvas.DrawText(nodeText, leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, infoFont, infoPaint);
                currentY += lineHeight;
            }

            // Draw navigation history count
            if (NavigationHistory.Count > 0)
            {
                string historyText = $"History: {NavigationHistory.Count} nodes";
                canvas.DrawText(historyText, leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, infoFont, infoPaint);
            }
        }

        /// <summary>
        /// Raises the NodeSelected event.
        /// </summary>
        /// <param name="node">The selected node.</param>
        protected virtual void OnNodeSelected(NetworkNode node)
        {
            NodeSelected?.Invoke(this, new NodeSelectionEventArgs(node));
        }

        /// <summary>
        /// Raises the ViewChanged event.
        /// </summary>
        protected virtual void OnViewChanged()
        {
            ViewChanged?.Invoke(this, new ViewChangedEventArgs(ZoomLevel, PanOffsetX, PanOffsetY));
        }
    }

    /// <summary>
    /// Event arguments for node selection events.
    /// </summary>
    public class NodeSelectionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the selected node.
        /// </summary>
        public NetworkNode SelectedNode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeSelectionEventArgs"/> class.
        /// </summary>
        /// <param name="selectedNode">The selected node.</param>
        public NodeSelectionEventArgs(NetworkNode selectedNode)
        {
            SelectedNode = selectedNode;
        }
    }

    /// <summary>
    /// Event arguments for view change events.
    /// </summary>
    public class ViewChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current zoom level.
        /// </summary>
        public float ZoomLevel { get; }

        /// <summary>
        /// Gets the pan offset X.
        /// </summary>
        public float PanOffsetX { get; }

        /// <summary>
        /// Gets the pan offset Y.
        /// </summary>
        public float PanOffsetY { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewChangedEventArgs"/> class.
        /// </summary>
        /// <param name="zoomLevel">The zoom level.</param>
        /// <param name="panOffsetX">The pan offset X.</param>
        /// <param name="panOffsetY">The pan offset Y.</param>
        public ViewChangedEventArgs(float zoomLevel, float panOffsetX, float panOffsetY)
        {
            ZoomLevel = zoomLevel;
            PanOffsetX = panOffsetX;
            PanOffsetY = panOffsetY;
        }
    }
}