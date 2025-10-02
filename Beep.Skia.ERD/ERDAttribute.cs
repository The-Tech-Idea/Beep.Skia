using SkiaSharp;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// ERD Attribute: ellipse with optional underline (for key) and double ellipse (for multivalued).
    /// </summary>
    public class ERDAttribute : ERDControl
    {
        private string _nameText = "Attribute";
        public string NameText { get => _nameText; set { if (_nameText == value) return; _nameText = value ?? string.Empty; InvalidateVisual(); } }
        private bool _isKey = false;
        public bool IsKey { get => _isKey; set { if (_isKey == value) return; _isKey = value; InvalidateVisual(); } }
        private bool _isMultivalued = false;
        public bool IsMultivalued { get => _isMultivalued; set { if (_isMultivalued == value) return; _isMultivalued = value; InvalidateVisual(); } }

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

        protected override void DrawERDContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            using var stroke = new SKPaint { Color = MaterialControl.MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var fill = new SKPaint { Color = MaterialControl.MaterialColors.Surface, IsAntialias = true };
            using var text = new SKPaint { Color = MaterialControl.MaterialColors.OnSurface, IsAntialias = true };
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
