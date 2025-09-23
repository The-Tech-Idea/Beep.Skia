using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// ERD Entity: rectangle with title band and attribute list area.
    /// </summary>
    public class ERDEntity : ERDControl
    {
        public string Title { get; set; } = "Entity";
        public string[] Attributes { get; set; } = new[] { "Id", "Name" };

        public ERDEntity()
        {
            Name = "ERD Entity";
            DisplayText = string.Empty;
            EnsurePortCounts(2, 2);
        }

        protected override void LayoutPorts()
        {
            // Leave space for title band at top; ports along straight vertical edges
            LayoutPortsVerticalSegments(topInset: 28f, bottomInset: 8f);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var rect = Bounds;
            float titleHeight = 24f;

            using var bodyFill = new SKPaint { Color = new SKColor(0xFA, 0xFA, 0xFA), IsAntialias = true };
            using var border = new SKPaint { Color = new SKColor(0x21, 0x21, 0x21), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var titleFill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Body and border
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, bodyFill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, border);

            // Title band
            var titleRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + titleHeight);
            canvas.DrawRoundRect(titleRect, CornerRadius, CornerRadius, titleFill);
            // Cover the bottom corners of title band to keep only top corners rounded
            using (var eraser = new SKPaint { BlendMode = SKBlendMode.Src })
            {
                var flat = new SKRect(rect.Left, rect.Top + titleHeight - 1, rect.Right, rect.Top + titleHeight);
                canvas.DrawRect(flat, titleFill);
            }

            // Title text
            var titleX = rect.MidX - font.MeasureText(Title, textPaint) / 2;
            var titleY = rect.Top + titleHeight - 6;
            canvas.DrawText(Title, titleX, titleY, SKTextAlign.Left, font, textPaint);

            // Attribute list
            float y = rect.Top + titleHeight + 18;
            foreach (var a in Attributes)
            {
                if (string.IsNullOrEmpty(a)) continue;
                var tx = rect.Left + 10;
                canvas.DrawText(a, tx, y, SKTextAlign.Left, font, textPaint);
                y += 18;
                if (y > rect.Bottom - 8) break;
            }

            DrawPorts(canvas);
        }
    }
}
