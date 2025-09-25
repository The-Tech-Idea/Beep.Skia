using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Task: rounded rectangle with optional progress bar and title.
    /// </summary>
    public class TaskNode : PMControl
    {
        /// <summary>
        /// The title displayed inside the task card.
        /// </summary>
        private string _title = "Task";
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
        private int _percentComplete;
        public int PercentComplete
        {
            get => _percentComplete;
            set
            {
                var v = System.Math.Clamp(value, 0, 100);
                if (_percentComplete != v)
                {
                    _percentComplete = v;
                    InvalidateVisual();
                }
            }
        }

        public TaskNode()
        {
            Name = "PM Task";
            Width = 160;
            Height = 64;
            InPortCount = 1;
            OutPortCount = 1;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var r = Bounds;
            using var bg = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var border = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            // Card
            var cardRect = r;
            canvas.DrawRoundRect(cardRect, CornerRadius, CornerRadius, bg);
            canvas.DrawRoundRect(cardRect, CornerRadius, CornerRadius, border);

            // Progress bar
            var barHeight = 8f;
            var barMargin = 10f;
            var barRect = new SKRect(r.Left + barMargin, r.Top + barMargin, r.Right - barMargin, r.Top + barMargin + barHeight);
            using var barBg = new SKPaint { Color = MaterialColors.SurfaceVariant, IsAntialias = true };
            using var barFg = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            canvas.DrawRoundRect(barRect, 4, 4, barBg);
            if (PercentComplete > 0)
            {
                var w = barRect.Width * (PercentComplete / 100f);
                var filled = new SKRect(barRect.Left, barRect.Top, barRect.Left + w, barRect.Bottom);
                canvas.DrawRoundRect(filled, 4, 4, barFg);
            }

            // Title centered
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            float tx = r.MidX;
            float ty = r.MidY + 8;
            canvas.DrawText(Title, tx, ty, SKTextAlign.Center, font, text);

            DrawPorts(canvas);
        }
    }
}
