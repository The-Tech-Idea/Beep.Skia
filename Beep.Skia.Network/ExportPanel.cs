using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Export panel component that provides functionality to export network data and visualizations.
    /// Supports various formats including CSV, JSON, GraphML, and image exports.
    /// </summary>
    public class ExportPanel : NetworkControl
    {
        /// <summary>
        /// Gets or sets the selected export format.
        /// </summary>
        public ExportFormat SelectedFormat { get; set; } = ExportFormat.CSV;

        /// <summary>
        /// Gets the list of available export formats.
        /// </summary>
        public List<ExportFormat> AvailableFormats { get; } = new List<ExportFormat>
        {
            ExportFormat.CSV,
            ExportFormat.JSON,
            ExportFormat.GraphML,
            ExportFormat.PNG,
            ExportFormat.SVG
        };

        /// <summary>
        /// Gets or sets the export options.
        /// </summary>
        public ExportOptions Options { get; set; } = new ExportOptions();

        /// <summary>
        /// Gets or sets whether an export is currently in progress.
        /// </summary>
        public bool IsExporting { get; set; } = false;

        /// <summary>
        /// Gets or sets the last export path.
        /// </summary>
        public string LastExportPath { get; set; }

        /// <summary>
        /// Gets or sets whether to show export options.
        /// </summary>
        public bool ShowOptions { get; set; } = true;

        /// <summary>
        /// Event raised when an export is completed.
        /// </summary>
        public event EventHandler<ExportCompletedEventArgs> ExportCompleted;

        /// <summary>
        /// Event raised when an export fails.
        /// </summary>
        public event EventHandler<ExportFailedEventArgs> ExportFailed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportPanel"/> class.
        /// </summary>
        public ExportPanel()
        {
            Width = 280;
            Height = 200;
            Name = "ExportPanel";
            DisplayText = "Export";
            TextPosition = TextPosition.Above;
            PrimaryColor = MaterialColors.Secondary;
        }

        /// <summary>
        /// Exports the network data using the current settings.
        /// </summary>
        /// <param name="nodes">The network nodes to export.</param>
        /// <param name="links">The network links to export.</param>
        /// <param name="filePath">The file path to export to.</param>
        public void Export(List<NetworkNode> nodes, List<NetworkLink> links, string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || IsExporting)
                return;

            IsExporting = true;
            LastExportPath = filePath;

            try
            {
                switch (SelectedFormat)
                {
                    case ExportFormat.CSV:
                        ExportToCSV(nodes, links, filePath);
                        break;
                    case ExportFormat.JSON:
                        ExportToJSON(nodes, links, filePath);
                        break;
                    case ExportFormat.GraphML:
                        ExportToGraphML(nodes, links, filePath);
                        break;
                    case ExportFormat.PNG:
                        ExportToPNG(nodes, links, filePath);
                        break;
                    case ExportFormat.SVG:
                        ExportToSVG(nodes, links, filePath);
                        break;
                }

                OnExportCompleted(filePath, SelectedFormat);
            }
            catch (Exception ex)
            {
                OnExportFailed(ex.Message);
            }
            finally
            {
                IsExporting = false;
            }
        }

        /// <summary>
        /// Exports network data to CSV format.
        /// </summary>
        private void ExportToCSV(List<NetworkNode> nodes, List<NetworkLink> links, string filePath)
        {
            var csv = new StringBuilder();

            // Write nodes
            csv.AppendLine("NodeID,Name,X,Y,Type,Community");
            foreach (var node in nodes)
            {
                csv.AppendLine($"{node.Id},{EscapeCSV(node.Name)},{node.X:F2},{node.Y:F2},{node.NodeType},{node.CommunityId}");
            }

            // Write links
            csv.AppendLine();
            csv.AppendLine("SourceID,TargetID,Weight,Type");
            foreach (var link in links)
            {
                csv.AppendLine($"{link.SourceNode.Id},{link.TargetNode.Id},{link.Weight:F2},{link.LinkType}");
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// Exports network data to JSON format.
        /// </summary>
        private void ExportToJSON(List<NetworkNode> nodes, List<NetworkLink> links, string filePath)
        {
            var json = new StringBuilder();
            json.AppendLine("{");

            // Export nodes
            json.AppendLine("  \"nodes\": [");
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                json.Append($"    {{\"id\": \"{node.Id}\", \"name\": \"{EscapeJSON(node.Name)}\", \"x\": {node.X:F2}, \"y\": {node.Y:F2}, \"type\": \"{node.NodeType}\", \"community\": {node.CommunityId}}}");
                json.AppendLine(i < nodes.Count - 1 ? "," : "");
            }
            json.AppendLine("  ],");

            // Export links
            json.AppendLine("  \"links\": [");
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                json.Append($"    {{\"source\": \"{link.SourceNode.Id}\", \"target\": \"{link.TargetNode.Id}\", \"weight\": {link.Weight:F2}, \"type\": \"{link.LinkType}\"}}");
                json.AppendLine(i < links.Count - 1 ? "," : "");
            }
            json.AppendLine("  ]");

            json.AppendLine("}");

            File.WriteAllText(filePath, json.ToString());
        }

        /// <summary>
        /// Exports network data to GraphML format.
        /// </summary>
        private void ExportToGraphML(List<NetworkNode> nodes, List<NetworkLink> links, string filePath)
        {
            var graphml = new StringBuilder();
            graphml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            graphml.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");

            // Define node attributes
            graphml.AppendLine("  <key id=\"name\" for=\"node\" attr.name=\"name\" attr.type=\"string\"/>");
            graphml.AppendLine("  <key id=\"x\" for=\"node\" attr.name=\"x\" attr.type=\"double\"/>");
            graphml.AppendLine("  <key id=\"y\" for=\"node\" attr.name=\"y\" attr.type=\"double\"/>");
            graphml.AppendLine("  <key id=\"type\" for=\"node\" attr.name=\"type\" attr.type=\"string\"/>");
            graphml.AppendLine("  <key id=\"community\" for=\"node\" attr.name=\"community\" attr.type=\"int\"/>");

            // Define edge attributes
            graphml.AppendLine("  <key id=\"weight\" for=\"edge\" attr.name=\"weight\" attr.type=\"double\"/>");
            graphml.AppendLine("  <key id=\"type\" for=\"edge\" attr.name=\"type\" attr.type=\"string\"/>");

            graphml.AppendLine("  <graph edgedefault=\"undirected\">");

            // Write nodes
            foreach (var node in nodes)
            {
                graphml.AppendLine($"    <node id=\"{node.Id}\">");
                graphml.AppendLine($"      <data key=\"name\">{EscapeXML(node.Name)}</data>");
                graphml.AppendLine($"      <data key=\"x\">{node.X:F2}</data>");
                graphml.AppendLine($"      <data key=\"y\">{node.Y:F2}</data>");
                graphml.AppendLine($"      <data key=\"type\">{node.NodeType}</data>");
                graphml.AppendLine($"      <data key=\"community\">{node.CommunityId}</data>");
                graphml.AppendLine("    </node>");
            }

            // Write edges
            foreach (var link in links)
            {
                graphml.AppendLine($"    <edge source=\"{link.SourceNode.Id}\" target=\"{link.TargetNode.Id}\">");
                graphml.AppendLine($"      <data key=\"weight\">{link.Weight:F2}</data>");
                graphml.AppendLine($"      <data key=\"type\">{link.LinkType}</data>");
                graphml.AppendLine("    </edge>");
            }

            graphml.AppendLine("  </graph>");
            graphml.AppendLine("</graphml>");

            File.WriteAllText(filePath, graphml.ToString());
        }

        /// <summary>
        /// Exports network visualization to PNG format.
        /// </summary>
        private void ExportToPNG(List<NetworkNode> nodes, List<NetworkLink> links, string filePath)
        {
            // This would require access to the rendering surface
            // For now, create a placeholder implementation
            using var bitmap = new SKBitmap(800, 600);
            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(SKColors.White);

            // Draw simple network representation
            using var nodePaint = new SKPaint { Color = SKColors.Blue, IsAntialias = true };
            using var linkPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };

            // Draw links
            foreach (var link in links)
            {
                canvas.DrawLine(link.SourceNode.X, link.SourceNode.Y,
                             link.TargetNode.X, link.TargetNode.Y, linkPaint);
            }

            // Draw nodes
            foreach (var node in nodes)
            {
                canvas.DrawCircle(node.X, node.Y, 5, nodePaint);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }

        /// <summary>
        /// Exports network visualization to SVG format.
        /// </summary>
        private void ExportToSVG(List<NetworkNode> nodes, List<NetworkLink> links, string filePath)
        {
            var svg = new StringBuilder();
            svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            svg.AppendLine("<svg width=\"800\" height=\"600\" xmlns=\"http://www.w3.org/2000/svg\">");

            // Draw links
            foreach (var link in links)
            {
                svg.AppendLine($"  <line x1=\"{link.SourceNode.X}\" y1=\"{link.SourceNode.Y}\" x2=\"{link.TargetNode.X}\" y2=\"{link.TargetNode.Y}\" stroke=\"gray\" stroke-width=\"1\"/>");
            }

            // Draw nodes
            foreach (var node in nodes)
            {
                svg.AppendLine($"  <circle cx=\"{node.X}\" cy=\"{node.Y}\" r=\"5\" fill=\"blue\"/>");
                svg.AppendLine($"  <text x=\"{node.X + 8}\" y=\"{node.Y + 4}\" font-size=\"10\" fill=\"black\">{EscapeXML(node.Name)}</text>");
            }

            svg.AppendLine("</svg>");

            File.WriteAllText(filePath, svg.ToString());
        }

        /// <summary>
        /// Escapes CSV special characters.
        /// </summary>
        private string EscapeCSV(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }

            return text;
        }

        /// <summary>
        /// Escapes JSON special characters.
        /// </summary>
        private string EscapeJSON(string text)
        {
            return text?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") ?? "";
        }

        /// <summary>
        /// Escapes XML special characters.
        /// </summary>
        private string EscapeXML(string text)
        {
            return text?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;") ?? "";
        }

        /// <summary>
        /// Handles mouse down events for export controls.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled, false otherwise.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (!Bounds.Contains(point))
                return false;

            // Handle format selection clicks
            float currentY = Y + 35;
            float buttonHeight = 18;
            float leftMargin = X + 10;

            for (int i = 0; i < AvailableFormats.Count; i++)
            {
                var buttonRect = new SKRect(leftMargin, currentY, X + Width - 10, currentY + buttonHeight);

                if (buttonRect.Contains(point))
                {
                    SelectedFormat = AvailableFormats[i];
                    return true;
                }

                currentY += buttonHeight + 2;
            }

            return false;
        }

        /// <summary>
        /// Draws the export panel control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw panel background
            DrawFilledRect(canvas, panelRect, MaterialColors.SurfaceContainer);

            if (!ShowOptions)
                return;

            float currentY = Y + 15;
            float lineHeight = 16;
            float leftMargin = X + 10;

            // Draw title
            using var titleFont = new SKFont { Size = 12 };
            using var titlePaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
            canvas.DrawText("Export Network", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, titleFont, titlePaint);

            currentY += lineHeight + 5;

            // Draw format buttons
            using var buttonFont = new SKFont { Size = 10 };

            float buttonHeight = 18;

            foreach (var format in AvailableFormats)
            {
                var buttonRect = new SKRect(leftMargin, currentY, X + Width - 10, currentY + buttonHeight);

                // Highlight selected format
                var paint = format == SelectedFormat ? PrimaryColor : MaterialColors.SurfaceVariant;
                DrawFilledRect(canvas, buttonRect, paint);

                // Draw format name
                string formatName = format.ToString();
                var textColor = paint == PrimaryColor ? MaterialColors.OnPrimary : MaterialColors.OnSurface;
                using var textPaintLocal = new SKPaint { Color = textColor, IsAntialias = true };
                canvas.DrawText(formatName, buttonRect.MidX, buttonRect.MidY + 3, SKTextAlign.Center, buttonFont, textPaintLocal);

                currentY += buttonHeight + 2;
            }

            // Draw export status
            using var statusFont = new SKFont { Size = 9 };
            SKPaint statusPaint;

            if (IsExporting)
            {
                statusPaint = new SKPaint { Color = MaterialColors.Tertiary, IsAntialias = true };
                canvas.DrawText("Exporting...", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, statusFont, statusPaint);
            }
            else if (!string.IsNullOrEmpty(LastExportPath))
            {
                statusPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
                string fileName = Path.GetFileName(LastExportPath);
                canvas.DrawText($"Exported: {fileName}", leftMargin, currentY + lineHeight - 3, SKTextAlign.Left, statusFont, statusPaint);
            }
        }

        /// <summary>
        /// Raises the ExportCompleted event.
        /// </summary>
        /// <param name="filePath">The export file path.</param>
        /// <param name="format">The export format.</param>
        protected virtual void OnExportCompleted(string filePath, ExportFormat format)
        {
            ExportCompleted?.Invoke(this, new ExportCompletedEventArgs(filePath, format));
        }

        /// <summary>
        /// Raises the ExportFailed event.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        protected virtual void OnExportFailed(string errorMessage)
        {
            ExportFailed?.Invoke(this, new ExportFailedEventArgs(errorMessage));
        }
    }

    /// <summary>
    /// Export format types.
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// CSV format.
        /// </summary>
        CSV,

        /// <summary>
        /// JSON format.
        /// </summary>
        JSON,

        /// <summary>
        /// GraphML format.
        /// </summary>
        GraphML,

        /// <summary>
        /// PNG image format.
        /// </summary>
        PNG,

        /// <summary>
        /// SVG vector format.
        /// </summary>
        SVG
    }

    /// <summary>
    /// Export options.
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// Gets or sets whether to include node positions.
        /// </summary>
        public bool IncludePositions { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include node attributes.
        /// </summary>
        public bool IncludeAttributes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include link weights.
        /// </summary>
        public bool IncludeWeights { get; set; } = true;

        /// <summary>
        /// Gets or sets the image resolution for image exports.
        /// </summary>
        public int ImageResolution { get; set; } = 300;

        /// <summary>
        /// Gets or sets whether to compress the output.
        /// </summary>
        public bool CompressOutput { get; set; } = false;
    }

    /// <summary>
    /// Event arguments for export completed events.
    /// </summary>
    public class ExportCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the export file path.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the export format.
        /// </summary>
        public ExportFormat Format { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="format">The export format.</param>
        public ExportCompletedEventArgs(string filePath, ExportFormat format)
        {
            FilePath = filePath;
            Format = format;
        }
    }

    /// <summary>
    /// Event arguments for export failed events.
    /// </summary>
    public class ExportFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportFailedEventArgs"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public ExportFailedEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}