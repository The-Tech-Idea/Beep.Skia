using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Network analyzer component that displays network metrics and statistics.
    /// Shows various network analysis data in a structured panel.
    /// </summary>
    public class NetworkAnalyzer : NetworkControl
    {
        /// <summary>
        /// Gets or sets the number of nodes in the network.
        /// </summary>
        public int NodeCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of links in the network.
        /// </summary>
        public int LinkCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the average degree of nodes in the network.
        /// </summary>
        public double AverageDegree { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the network density.
        /// </summary>
        public double Density { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the number of connected components.
        /// </summary>
        public int ConnectedComponents { get; set; } = 0;

        /// <summary>
        /// Gets or sets the average clustering coefficient.
        /// </summary>
        public double ClusteringCoefficient { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the network diameter.
        /// </summary>
        public int Diameter { get; set; } = 0;

        /// <summary>
        /// Gets or sets the background color for the analyzer panel.
        /// </summary>
    public SKColor PanelBackground { get; set; } = MaterialColors.SurfaceContainer;

        /// <summary>
        /// Gets or sets the header background color.
        /// </summary>
    public SKColor HeaderBackground { get; set; } = MaterialColors.Primary;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkAnalyzer"/> class.
        /// </summary>
        public NetworkAnalyzer()
        {
            Width = 280;
            Height = 200;
            Name = "NetworkAnalyzer";
            DisplayText = "Network Analysis";
            TextPosition = TextPosition.Above;
            PrimaryColor = MaterialColors.Primary;
        }

        /// <summary>
        /// Draws the network analyzer panel with metrics.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, PanelBackground);

            // Draw header
            var headerRect = new SKRect(X, Y, X + Width, Y + 30);
            DrawFilledRect(canvas, headerRect, HeaderBackground);

            // Draw header text
            DrawCenteredText(canvas, "Network Metrics", headerRect, 14, MaterialColors.OnPrimary);

            // Draw metrics
            float currentY = Y + 40;
            float lineHeight = 18;
            float leftMargin = X + 10;

            DrawMetricLine(canvas, "Nodes:", NodeCount.ToString(), leftMargin, currentY, lineHeight);
            currentY += lineHeight;
            DrawMetricLine(canvas, "Links:", LinkCount.ToString(), leftMargin, currentY, lineHeight);
            currentY += lineHeight;
            DrawMetricLine(canvas, "Avg Degree:", AverageDegree.ToString("F2"), leftMargin, currentY, lineHeight);
            currentY += lineHeight;
            DrawMetricLine(canvas, "Density:", Density.ToString("F3"), leftMargin, currentY, lineHeight);
            currentY += lineHeight;
            DrawMetricLine(canvas, "Components:", ConnectedComponents.ToString(), leftMargin, currentY, lineHeight);
            currentY += lineHeight;
            DrawMetricLine(canvas, "Clustering:", ClusteringCoefficient.ToString("F3"), leftMargin, currentY, lineHeight);
            currentY += lineHeight;
            DrawMetricLine(canvas, "Diameter:", Diameter.ToString(), leftMargin, currentY, lineHeight);
        }

        /// <summary>
        /// Draws a metric line with label and value.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="label">The metric label.</param>
        /// <param name="value">The metric value.</param>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="lineHeight">The line height.</param>
        private void DrawMetricLine(SKCanvas canvas, string label, string value, float x, float y, float lineHeight)
        {
            using var font = new SKFont { Size = 11 };
            using var labelPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var valuePaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };

            canvas.DrawText(label, x, y + lineHeight - 3, SKTextAlign.Left, font, labelPaint);
            canvas.DrawText(value, x + 120, y + lineHeight - 3, SKTextAlign.Left, font, valuePaint);
        }
    }
}