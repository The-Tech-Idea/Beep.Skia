using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// ERD Attribute: ellipse with optional underline (for key) and double ellipse (for multivalued).
    /// </summary>
    public class ERDAttribute : ERDControl
    {
        public string NameText { get; set; } = "Attribute";
        public bool IsKey { get; set; } = false;
        public bool IsMultivalued { get; set; } = false;

        public ERDAttribute()
        {
            Name = "ERD Attribute";
            DisplayText = string.Empty;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Ports on the ellipse mid-left and mid-right
            var b = Bounds;
            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                cp.Center = new SKPoint(b.Left - 2, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cp.Center.X - PortRadius, cp.Center.Y - PortRadius, cp.Center.X + PortRadius, cp.Center.Y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0; cp.Component = this; cp.IsAvailable = true;
            }
            if (OutConnectionPoints.Count > 0)
            {
                var cp = OutConnectionPoints[0];
                cp.Center = new SKPoint(b.Right + 2, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cp.Center.X - PortRadius, cp.Center.Y - PortRadius, cp.Center.X + PortRadius, cp.Center.Y + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0; cp.Component = this; cp.IsAvailable = true;
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            LayoutPorts();

            var b = Bounds;
            using var stroke = new SKPaint { Color = new SKColor(0x19, 0x76, 0xD2), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var fill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            canvas.DrawOval(b, fill);
            canvas.DrawOval(b, stroke);

            if (IsMultivalued)
            {
                var inset = 6f;
                var inner = new SKRect(b.Left + inset, b.Top + inset, b.Right - inset, b.Bottom - inset);
                canvas.DrawOval(inner, stroke);
            }

            var tx = b.MidX - font.MeasureText(NameText, text) / 2;
            var ty = b.MidY + 5;
            canvas.DrawText(NameText, tx, ty, SKTextAlign.Left, font, text);
            if (IsKey)
            {
                float w = font.MeasureText(NameText, text);
                using var keyStroke = new SKPaint { Color = text.Color, StrokeWidth = 1.5f, IsAntialias = true };
                canvas.DrawLine(tx, ty + 2, tx + w, ty + 2, keyStroke);
            }

            DrawPorts(canvas);
        }
    }
}
