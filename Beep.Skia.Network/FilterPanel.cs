using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Filter panel component that provides filtering and search capabilities for network elements.
    /// Supports node/link filtering by properties, search queries, and visibility controls.
    /// </summary>
    public class FilterPanel : NetworkControl
    {
        /// <summary>
        /// Gets or sets the search query text.
        /// </summary>
        public string SearchQuery { get; set; } = "";

        /// <summary>
        /// Gets or sets whether the search is case-sensitive.
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use regular expressions for search.
        /// </summary>
        public bool UseRegex { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum node degree filter.
        /// </summary>
        public int MinDegree { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum node degree filter.
        /// </summary>
        public int MaxDegree { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets whether to show only connected nodes.
        /// </summary>
        public bool ShowConnectedOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show isolated nodes (degree = 0).
        /// </summary>
        public bool ShowIsolatedNodes { get; set; } = true;

        /// <summary>
        /// Gets the list of filtered nodes.
        /// </summary>
        public List<NetworkNode> FilteredNodes { get; } = new List<NetworkNode>();

        /// <summary>
        /// Gets the list of filtered links.
        /// </summary>
        public List<NetworkLink> FilteredLinks { get; } = new List<NetworkLink>();

        /// <summary>
        /// Gets or sets whether to show filter controls.
        /// </summary>
        public bool ShowControls { get; set; } = true;

        /// <summary>
        /// Event raised when the filter changes.
        /// </summary>
        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterPanel"/> class.
        /// </summary>
        public FilterPanel()
        {
            Width = 280;
            Height = 200;
            Name = "FilterPanel";
            DisplayText = "Filters";
            TextPosition = TextPosition.Above;
            PrimaryColor = MaterialColors.Tertiary;
        }

        /// <summary>
        /// Applies filters to the given network data.
        /// </summary>
        /// <param name="nodes">The network nodes.</param>
        /// <param name="links">The network links.</param>
        public void ApplyFilters(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            FilteredNodes.Clear();
            FilteredLinks.Clear();

            if (nodes == null || links == null)
                return;

            // Calculate node degrees
            var nodeDegrees = CalculateNodeDegrees(nodes, links);

            // Filter nodes
            foreach (var node in nodes)
            {
                bool passesFilter = true;

                // Search query filter
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    passesFilter &= MatchesSearchQuery(node.Name, SearchQuery);
                }

                // Degree filter
                int degree = nodeDegrees.GetValueOrDefault(node, 0);
                passesFilter &= degree >= MinDegree && degree <= MaxDegree;

                // Connected/isolated filter
                if (ShowConnectedOnly && degree == 0)
                    passesFilter = false;
                if (!ShowIsolatedNodes && degree == 0)
                    passesFilter = false;

                if (passesFilter)
                {
                    FilteredNodes.Add(node);
                }
            }

            // Filter links (only include links between filtered nodes)
            foreach (var link in links)
            {
                if (FilteredNodes.Contains(link.SourceNode) && FilteredNodes.Contains(link.TargetNode))
                {
                    FilteredLinks.Add(link);
                }
            }

            OnFilterChanged();
        }

        /// <summary>
        /// Clears all filters and shows all elements.
        /// </summary>
        public void ClearFilters()
        {
            SearchQuery = "";
            MinDegree = 0;
            MaxDegree = int.MaxValue;
            ShowConnectedOnly = false;
            ShowIsolatedNodes = true;

            OnFilterChanged();
        }

        /// <summary>
        /// Sets a search query and applies filters.
        /// </summary>
        /// <param name="query">The search query.</param>
        public void SetSearchQuery(string query)
        {
            SearchQuery = query ?? "";
            // Re-apply filters if we have cached data
            if (FilteredNodes.Count > 0 || FilteredLinks.Count > 0)
            {
                // This would need to be called with the original data
                OnFilterChanged();
            }
        }

        /// <summary>
        /// Sets degree range filter.
        /// </summary>
        /// <param name="minDegree">The minimum degree.</param>
        /// <param name="maxDegree">The maximum degree.</param>
        public void SetDegreeRange(int minDegree, int maxDegree)
        {
            MinDegree = Math.Max(0, minDegree);
            MaxDegree = Math.Max(MinDegree, maxDegree);
            OnFilterChanged();
        }

        /// <summary>
        /// Calculates the degree of each node.
        /// </summary>
        private Dictionary<NetworkNode, int> CalculateNodeDegrees(List<NetworkNode> nodes, List<NetworkLink> links)
        {
            var degrees = new Dictionary<NetworkNode, int>();

            foreach (var node in nodes)
            {
                degrees[node] = 0;
            }

            foreach (var link in links)
            {
                if (degrees.ContainsKey(link.SourceNode))
                    degrees[link.SourceNode]++;
                if (degrees.ContainsKey(link.TargetNode))
                    degrees[link.TargetNode]++;
            }

            return degrees;
        }

        /// <summary>
        /// Checks if a text matches the search query.
        /// </summary>
        private bool MatchesSearchQuery(string text, string query)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return string.IsNullOrEmpty(query); // Empty query matches everything

            string searchText = CaseSensitive ? text : text.ToLower();
            string searchQuery = CaseSensitive ? query : query.ToLower();

            if (UseRegex)
            {
                try
                {
                    return Regex.IsMatch(searchText, searchQuery);
                }
                catch
                {
                    // Invalid regex, fall back to simple contains
                    return searchText.Contains(searchQuery);
                }
            }
            else
            {
                return searchText.Contains(searchQuery);
            }
        }

        /// <summary>
        /// Handles mouse down events for filter controls.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (!Bounds.Contains(point))
                return false;

            // Handle control clicks (simplified - in a real implementation,
            // this would handle text input and checkbox interactions)
            float currentY = Y + 80;
            float checkBoxSize = 12;
            float leftMargin = X + 10;

            // Connected only checkbox
            var connectedRect = new SKRect(leftMargin, currentY - 2, leftMargin + checkBoxSize, currentY + checkBoxSize - 2);
            if (connectedRect.Contains(point))
            {
                ShowConnectedOnly = !ShowConnectedOnly;
                OnFilterChanged();
                return true;
            }

            currentY += 18;

            // Isolated nodes checkbox
            var isolatedRect = new SKRect(leftMargin, currentY - 2, leftMargin + checkBoxSize, currentY + checkBoxSize - 2);
            if (isolatedRect.Contains(point))
            {
                ShowIsolatedNodes = !ShowIsolatedNodes;
                OnFilterChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Draws the filter panel control.
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
            canvas.DrawText("Network Filters", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, titleFont, titlePaint);

            currentY += lineHeight + 5;

            // Draw search box
            using var controlFont = new SKFont { Size = 10 };
            using var controlPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var boxPaint = new SKPaint { Color = MaterialColors.SurfaceVariant, IsAntialias = true };

            var searchRect = new SKRect(leftMargin, currentY, X + Width - 10, currentY + 20);
            DrawFilledRect(canvas, searchRect, MaterialColors.SurfaceVariant);

            string displayQuery = string.IsNullOrEmpty(SearchQuery) ? "Search nodes..." : SearchQuery;
            canvas.DrawText(displayQuery, leftMargin + 5, currentY + 14, SKTextAlign.Left, controlFont, controlPaint);

            currentY += 28;

            // Draw degree filter
            canvas.DrawText($"Degree: {MinDegree} - {MaxDegree}", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, controlFont, controlPaint);
            currentY += lineHeight;

            // Draw checkboxes
            float checkBoxSize = 12;
            using var checkPaint = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true };
            using var checkFillPaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };

            // Connected only checkbox
            var connectedRect = new SKRect(leftMargin, currentY - 2, leftMargin + checkBoxSize, currentY + checkBoxSize - 2);
            DrawFilledRect(canvas, connectedRect, MaterialColors.Outline);
            if (ShowConnectedOnly)
            {
                var innerRect = new SKRect(connectedRect.Left + 2, connectedRect.Top + 2,
                                         connectedRect.Right - 2, connectedRect.Bottom - 2);
                DrawFilledRect(canvas, innerRect, PrimaryColor);
            }
            canvas.DrawText("Connected nodes only", leftMargin + 16, currentY + 9, SKTextAlign.Left, controlFont, controlPaint);

            currentY += 18;

            // Isolated nodes checkbox
            var isolatedRect = new SKRect(leftMargin, currentY - 2, leftMargin + checkBoxSize, currentY + checkBoxSize - 2);
            DrawFilledRect(canvas, isolatedRect, MaterialColors.Outline);
            if (ShowIsolatedNodes)
            {
                var innerRect = new SKRect(isolatedRect.Left + 2, isolatedRect.Top + 2,
                                         isolatedRect.Right - 2, isolatedRect.Bottom - 2);
                DrawFilledRect(canvas, innerRect, PrimaryColor);
            }
            canvas.DrawText("Show isolated nodes", leftMargin + 16, currentY + 9, SKTextAlign.Left, controlFont, controlPaint);

            currentY += 20;

            // Draw filter summary
            using var summaryFont = new SKFont { Size = 9 };
            using var summaryPaint = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true };

            string summaryText = $"{FilteredNodes.Count} nodes, {FilteredLinks.Count} links";
            canvas.DrawText(summaryText, leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, summaryFont, summaryPaint);
        }

        /// <summary>
        /// Raises the FilterChanged event.
        /// </summary>
        protected virtual void OnFilterChanged()
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(FilteredNodes, FilteredLinks));
        }
    }

    /// <summary>
    /// Event arguments for filter change events.
    /// </summary>
    public class FilterChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the filtered nodes.
        /// </summary>
        public IReadOnlyList<NetworkNode> FilteredNodes { get; }

        /// <summary>
        /// Gets the filtered links.
        /// </summary>
        public IReadOnlyList<NetworkLink> FilteredLinks { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterChangedEventArgs"/> class.
        /// </summary>
        /// <param name="filteredNodes">The filtered nodes.</param>
        /// <param name="filteredLinks">The filtered links.</param>
        public FilterChangedEventArgs(IReadOnlyList<NetworkNode> filteredNodes, IReadOnlyList<NetworkLink> filteredLinks)
        {
            FilteredNodes = filteredNodes;
            FilteredLinks = filteredLinks;
        }
    }
}