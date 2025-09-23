using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// Subsystem/Package container with title bar; no ports.
    /// </summary>
    public class DFDSubsystemContainer : DFDControl
    {
        public string Title { get; set; } = "Subsystem";
        private const float TitleBarHeight = 24f;

        public DFDSubsystemContainer()
        {
            Name = "Subsystem Container";
            DisplayText = string.Empty;
            TextPosition = Beep.Skia.TextPosition.Above;
            EnsurePortCounts(0, 0);
        }

        protected override void LayoutPorts() { /* no ports */ }

    protected override void DrawContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            var r = Bounds;
            using var bodyFill = new SKPaint { Color = new SKColor(0xFA, 0xFA, 0xFA), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x90, 0xA4, 0xAE), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var titleFill = new SKPaint { Color = new SKColor(0xCF, 0xD8, 0xDC), IsAntialias = true };
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont { Size = 14 };

            // Body and title bar
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, bodyFill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);
            var titleRect = new SKRect(r.Left, r.Top, r.Right, r.Top + TitleBarHeight);
            canvas.DrawRoundRect(titleRect, CornerRadius, CornerRadius, titleFill);

            // Title text
            canvas.DrawText(Title ?? string.Empty, r.Left + 8, r.Top + TitleBarHeight / 2 + 5, SKTextAlign.Left, font, textPaint);
        }
    }
}
