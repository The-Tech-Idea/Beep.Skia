using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    public class CentralNode : MindMapControl
    {
        public string Title { get; set; } = "Central Topic";
        public string? Notes { get; set; }

        public CentralNode()
        {
            Width = 220; Height = 120;
            BackgroundColor = MaterialColors.PrimaryContainer;
            BorderColor = MaterialColors.Primary;
            TextColor = MaterialColors.OnPrimaryContainer;
            EnsurePortCounts(0, 6);
        }

        protected override void LayoutPorts()
        {
            LayoutOutputsOnEllipse();
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, Height / 2f, Height / 2f, fill);
            canvas.DrawRoundRect(rect, Height / 2f, Height / 2f, stroke);

            using var font = new SKFont(SKTypeface.Default, 14) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);

            if (!string.IsNullOrWhiteSpace(Notes))
            {
                using var font2 = new SKFont(SKTypeface.Default, 11);
                using var t2 = new SKPaint { Color = MaterialColors.OnSurfaceVariant, IsAntialias = true };
                canvas.DrawText(Notes!.Length > 120 ? Notes!.Substring(0, 120) + "…" : Notes!, X + 12, Y + Height - 12, font2, t2);
            }

            DrawConnectionPoints(canvas);
        }
    }
}
