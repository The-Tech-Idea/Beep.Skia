using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Summary/Phase: bracketed bar indicating a group span.
    /// </summary>
    public class SummaryNode : PMControl
    {
        /// <summary>
        /// The title drawn above the summary bracket.
        /// </summary>
        private string _title = "Summary";
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_title, v, System.StringComparison.Ordinal))
                {
                    _title = v;
                    InvalidateVisual();
                }
            }
        }

        public SummaryNode()
        {
            Name = "PM Summary";
            Width = 200;
            Height = 50;
            InPortCount = 1;
            OutPortCount = 1;
        }

        protected override void LayoutPorts()
        {
            // Summary bracket: ports on left/right ends of the horizontal bracket
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var r = Bounds;
            using var stroke = new SKPaint { Color = MaterialColors.Secondary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Draw bracketed bar: left and right brackets with a connecting line
            float y = r.MidY;
            float leftX = r.Left + 8;
            float rightX = r.Right - 8;
            float bracketHeight = r.Height * 0.6f;
            float half = bracketHeight / 2f;

            // Left bracket
            canvas.DrawLine(leftX, y - half, leftX, y + half, stroke);
            canvas.DrawLine(leftX, y - half, leftX + 16, y - half, stroke);
            canvas.DrawLine(leftX, y + half, leftX + 16, y + half, stroke);

            // Right bracket
            canvas.DrawLine(rightX, y - half, rightX, y + half, stroke);
            canvas.DrawLine(rightX - 16, y - half, rightX, y - half, stroke);
            canvas.DrawLine(rightX - 16, y + half, rightX, y + half, stroke);

            // Connector
            canvas.DrawLine(leftX + 16, y, rightX - 16, y, stroke);

            // Title centered above
            canvas.DrawText(Title, r.MidX, r.Top + 16, SKTextAlign.Center, font, text);

            DrawPorts(canvas);
        }
    }
}
