using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Comment node: fold-over corner rectangle for additional notes or comments.
    /// Similar to AnnotationNode but with a distinctive folded corner appearance.
    /// </summary>
    public class CommentNode : FlowchartControl
    {
        private string _text = "Comment";
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

        public CommentNode()
        {
            Name = "Flowchart Comment";
            Width = 160;
            Height = 100;
            EnsurePortCounts(0, 0); // Comments typically don't connect

            NodeProperties["Text"] = new ParameterInfo
            {
                ParameterName = "Text",
                ParameterType = typeof(string),
                DefaultParameterValue = _text,
                ParameterCurrentValue = _text,
                Description = "Comment text (multi-line supported)."
            };
        }

        protected override void LayoutPorts()
        {
            // No ports for comments
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float foldSize = 20f;

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFF, 0xFF, 0xE0), IsAntialias = true }; // Light yellow
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xE0, 0xE0, 0xE0), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 }; // Light gray
            using var text = new SKPaint { Color = CustomTextColor ?? new SKColor(0x60, 0x60, 0x60), IsAntialias = true }; // Gray text
            using var font = new SKFont(SKTypeface.Default, 12);

            // Main rectangle body
            using var bodyPath = new SKPath();
            bodyPath.MoveTo(r.Left, r.Top);
            bodyPath.LineTo(r.Right - foldSize, r.Top);
            bodyPath.LineTo(r.Right, r.Top + foldSize);
            bodyPath.LineTo(r.Right, r.Bottom);
            bodyPath.LineTo(r.Left, r.Bottom);
            bodyPath.Close();

            canvas.DrawPath(bodyPath, fill);
            canvas.DrawPath(bodyPath, stroke);

            // Folded corner
            using var foldPath = new SKPath();
            foldPath.MoveTo(r.Right - foldSize, r.Top);
            foldPath.LineTo(r.Right - foldSize, r.Top + foldSize);
            foldPath.LineTo(r.Right, r.Top + foldSize);
            foldPath.Close();

            using var foldFill = new SKPaint { Color = new SKColor(0xF0, 0xF0, 0xD0), IsAntialias = true }; // Slightly darker yellow
            canvas.DrawPath(foldPath, foldFill);
            canvas.DrawPath(foldPath, stroke);

            // Draw text with word wrapping
            DrawWrappedText(canvas, Text, r.Left + 8, r.Top + 8, r.Width - 16, r.Height - 16, font, text);
        }

        private void DrawWrappedText(SKCanvas canvas, string text, float x, float y, float maxWidth, float maxHeight, SKFont font, SKPaint paint)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var words = text.Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            string currentLine = "";
            float lineHeight = 16f;
            float currentY = y + lineHeight;

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float testWidth = font.MeasureText(testLine, paint);

                if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    canvas.DrawText(currentLine, x, currentY, SKTextAlign.Left, font, paint);
                    currentLine = word;
                    currentY += lineHeight;

                    if (currentY > y + maxHeight) break;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine) && currentY <= y + maxHeight)
            {
                canvas.DrawText(currentLine, x, currentY, SKTextAlign.Left, font, paint);
            }
        }
    }
}
