using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    public class SubTopicNode : MindMapControl
    {
        public string Title { get; set; } = "SubTopic";
        public string? Notes { get; set; }

        public SubTopicNode()
        {
            Width = 140; Height = 56;
            BackgroundColor = MaterialColors.SurfaceVariant;
            BorderColor = MaterialColors.OutlineVariant;
            TextColor = MaterialColors.OnSurfaceVariant;
            EnsurePortCounts(1, 2);
        }

        protected override void LayoutPorts()
        {
            // Use default right-edge layout for rectangular subtopics
            base.LayoutPorts();
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fill);
            canvas.DrawRoundRect(rect, 8, 8, stroke);

            using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);
            DrawConnectionPoints(canvas);
        }
    }
}
