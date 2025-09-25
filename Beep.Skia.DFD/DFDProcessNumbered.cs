using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process with an ID bubble inside.
    /// </summary>
    public class DFDProcessNumbered : DFDControl
    {
        public string ProcessId { get; set; } = "1";

        public DFDProcessNumbered()
        {
            Name = "Process (Numbered)";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);
        }

        protected override void LayoutPorts()
        {
            // Rounded rect avoidance
            LayoutPortsVerticalSegments(topInset: CornerRadius, bottomInset: CornerRadius);
        }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            LayoutPorts();

            var rect = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            // ID bubble near top-left inside body
            float bubbleR = 14f;
            var bubbleCenter = new SKPoint(rect.Left + bubbleR + 8f, rect.Top + bubbleR + 8f);
            using var bubbleFill = new SKPaint { Color = MaterialColors.SurfaceContainer, IsAntialias = true };
            using var bubbleStroke = new SKPaint { Color = MaterialColors.OutlineVariant, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var textPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var font = new SKFont { Size = 12 };

            canvas.DrawCircle(bubbleCenter, bubbleR, bubbleFill);
            canvas.DrawCircle(bubbleCenter, bubbleR, bubbleStroke);
            canvas.DrawText(ProcessId ?? string.Empty, bubbleCenter.X, bubbleCenter.Y + 4, SKTextAlign.Center, font, textPaint);

            DrawPorts(canvas);
        }
    }
}
