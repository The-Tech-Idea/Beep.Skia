using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.Network
{
    // Simple network node component; discoverable via SkiaComponentRegistry
    public class NetworkNode : SkiaComponent
    {
        public SKColor FillColor { get; set; } = new SKColor(0x42, 0xA5, 0xF5); // Material Blue 400
        public SKColor StrokeColor { get; set; } = SKColors.Black;
        public float StrokeWidth { get; set; } = 1.5f;
        public float CornerRadius { get; set; } = 6f;

        public NetworkNode()
        {
            Width = 100;
            Height = 48;
            Name = "Node";
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = FillColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = StrokeColor, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            // label
            using var text = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var font = new SKFont { Size = 14 };
            var label = string.IsNullOrWhiteSpace(Name) ? "Node" : Name;
            var tb = new SKRect();
            font.MeasureText(label, out tb);
            float tx = X + (Width - tb.Width) / 2f;
            float ty = Y + (Height + tb.Height) / 2f - 3f;
            canvas.DrawText(label, tx, ty, SKTextAlign.Left, font, text);
        }
    }
}
