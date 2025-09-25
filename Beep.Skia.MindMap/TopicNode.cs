using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    public class TopicNode : MindMapControl
    {
        public string Title { get; set; } = "Topic";
        public string? Notes { get; set; }

        public TopicNode()
        {
            Width = 160; Height = 64;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Outline;
            TextColor = MaterialColors.OnSurface;
            EnsurePortCounts(1, 3);
        }

        protected override void LayoutPorts()
        {
            // Use default right-edge layout for rectangular topics
            base.LayoutPorts();
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 10, 10, fill);
            canvas.DrawRoundRect(rect, 10, 10, stroke);

            using var font = new SKFont(SKTypeface.Default, 13) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);

            DrawConnectionPoints(canvas);
        }
    }
}
