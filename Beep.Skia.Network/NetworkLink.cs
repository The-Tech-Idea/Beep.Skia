using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.Network
{
    // Simple visual link; not using IConnectionLine yet for simplicity
    public class NetworkLink : SkiaComponent
    {
        public SKPoint Start { get; set; }
        public SKPoint End { get; set; }
        public SKColor Color { get; set; } = SKColors.DarkSlateGray;
        public float Thickness { get; set; } = 2f;

        public NetworkLink()
        {
            Width = 0; Height = 0; // not used; draws by absolute coords
            Name = "Link";
            IsStatic = false; // links can be moved if desired
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var paint = new SKPaint { Color = Color, Style = SKPaintStyle.Stroke, StrokeWidth = Thickness, IsAntialias = true };
            // Simple cubic curve for aesthetics
            var dx = (End.X - Start.X) * 0.33f;
            var c1 = new SKPoint(Start.X + dx, Start.Y);
            var c2 = new SKPoint(End.X - dx, End.Y);
            using var path = new SKPath();
            path.MoveTo(Start);
            path.CubicTo(c1, c2, End);
            canvas.DrawPath(path, paint);
        }
    }
}
