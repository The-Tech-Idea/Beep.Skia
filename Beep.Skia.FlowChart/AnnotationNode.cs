using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Annotation node: dashed border rectangle for adding notes and comments to flowcharts.
    /// Can optionally attach to another node via a dotted connector line.
    /// </summary>
    public class AnnotationNode : FlowchartControl
    {
        private string _text = "Annotation";
        public string Text
        {
            get => _text;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_text, v, System.StringComparison.Ordinal))
                {
                    _text = v;
                    if (NodeProperties.TryGetValue("Text", out var pi))
                        pi.ParameterCurrentValue = _text;
                    InvalidateVisual();
                }
            }
        }

        private bool _showConnector = true;
        public bool ShowConnector
        {
            get => _showConnector;
            set
            {
                if (_showConnector != value)
                {
                    _showConnector = value;
                    if (NodeProperties.TryGetValue("ShowConnector", out var pi))
                        pi.ParameterCurrentValue = value;
                    // Adjust port count based on connector visibility
                    EnsurePortCounts(_showConnector ? 1 : 0, 0);
                    InvalidateVisual();
                }
            }
        }

        public AnnotationNode()
        {
            Name = "Flowchart Annotation";
            Width = 180;
            Height = 100;
            EnsurePortCounts(1, 0); // Input connector for attaching to nodes

            NodeProperties["Text"] = new ParameterInfo
            {
                ParameterName = "Text",
                ParameterType = typeof(string),
                DefaultParameterValue = _text,
                ParameterCurrentValue = _text,
                Description = "Annotation text (supports multi-line)."
            };
            NodeProperties["ShowConnector"] = new ParameterInfo
            {
                ParameterName = "ShowConnector",
                ParameterType = typeof(bool),
                DefaultParameterValue = _showConnector,
                ParameterCurrentValue = _showConnector,
                Description = "If true, shows connection port for attaching to another node."
            };
        }

        protected override void LayoutPorts()
        {
            if (!ShowConnector || InConnectionPoints.Count == 0) return;

            var r = Bounds;
            // Connector port on left side (centered)
            var connPt = InConnectionPoints[0];
            connPt.Center = new SKPoint(r.Left, r.MidY);
            connPt.Position = new SKPoint(r.Left - PortRadius, r.MidY);
            connPt.Bounds = new SKRect(
                connPt.Center.X - PortRadius,
                connPt.Center.Y - PortRadius,
                connPt.Center.X + PortRadius,
                connPt.Center.Y + PortRadius
            );
            connPt.Rect = connPt.Bounds;
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;

            // Dashed border style
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xFF, 0xF0), IsAntialias = true }; // Light yellow
            using var stroke = new SKPaint
            {
                Color = new SKColor(0x9E, 0x9E, 0x9E), // Gray
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                PathEffect = SKPathEffect.CreateDash(new float[] { 6, 3 }, 0)
            };
            using var text = new SKPaint { Color = new SKColor(0x42, 0x42, 0x42), IsAntialias = true }; // Dark gray text
            using var font = new SKFont(SKTypeface.Default, 12);

            canvas.DrawRoundRect(r, 4f, 4f, fill);
            canvas.DrawRoundRect(r, 4f, 4f, stroke);

            // Draw text with word wrapping
            DrawWrappedText(canvas, Text, r.Left + 8, r.Top + 8, r.Width - 16, r.Height - 16, font, text);

            if (ShowConnector)
                DrawPorts(canvas);
        }

        private void DrawWrappedText(SKCanvas canvas, string text, float x, float y, float maxWidth, float maxHeight, SKFont font, SKPaint paint)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var words = text.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            float lineHeight = font.Size * 1.4f;
            float currentY = y + font.Size;
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float testWidth = font.MeasureText(testLine, paint);

                if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    // Draw current line
                    canvas.DrawText(currentLine, x, currentY, SKTextAlign.Left, font, paint);
                    currentY += lineHeight;
                    currentLine = word;

                    if (currentY > y + maxHeight) break; // Exceeded height
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Draw last line
            if (!string.IsNullOrEmpty(currentLine) && currentY <= y + maxHeight)
            {
                canvas.DrawText(currentLine, x, currentY, SKTextAlign.Left, font, paint);
            }
        }
    }
}
