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

        // Additional properties for advanced network functionality
        public NetworkNode SourceNode { get; set; }
        public NetworkNode TargetNode { get; set; }
        public double Weight { get; set; } = 1.0;
        public string LinkType { get; set; } = "Default";
        public bool IsHighlighted { get; set; } = false;

        public NetworkLink()
        {
            Width = 0; Height = 0; // not used; draws by absolute coords
            Name = "Link";
            IsStatic = false; // links can be moved if desired
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Update start/end points from nodes if available
            if (SourceNode != null)
            {
                Start = new SKPoint(SourceNode.X + SourceNode.Width / 2, SourceNode.Y + SourceNode.Height / 2);
            }
            if (TargetNode != null)
            {
                End = new SKPoint(TargetNode.X + TargetNode.Width / 2, TargetNode.Y + TargetNode.Height / 2);
            }

            // Determine effective color
            SKColor effectiveColor = Color;
            if (IsHighlighted)
            {
                effectiveColor = new SKColor(0xFF, 0x98, 0x00); // Orange highlight
            }

            using var paint = new SKPaint { Color = effectiveColor, Style = SKPaintStyle.Stroke, StrokeWidth = Thickness, IsAntialias = true };
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
