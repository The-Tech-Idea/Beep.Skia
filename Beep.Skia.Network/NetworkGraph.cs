using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    public class NetworkGraph : SkiaComponent
    {
        public SKColor Background { get; set; } = new SKColor(250, 250, 250);
        public SKColor GridColor { get; set; } = new SKColor(230, 230, 230);
        public float GridSpacing { get; set; } = 24f;

        public List<NetworkNode> Nodes { get; } = new List<NetworkNode>();
        public List<NetworkLink> Links { get; } = new List<NetworkLink>();

        public NetworkGraph()
        {
            Width = 800;
            Height = 600;
            Name = "NetworkGraph";
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // background
            using var bg = new SKPaint { Color = Background, Style = SKPaintStyle.Fill };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRect(rect, bg);
            // grid
            using var grid = new SKPaint { Color = GridColor, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            for (float gx = X; gx <= X + Width; gx += GridSpacing)
                canvas.DrawLine(gx, Y, gx, Y + Height, grid);
            for (float gy = Y; gy <= Y + Height; gy += GridSpacing)
                canvas.DrawLine(X, gy, X + Width, gy, grid);

            // links beneath nodes
            foreach (var l in Links)
            {
                l.Draw(canvas, context);
            }
            // nodes
            foreach (var n in Nodes)
            {
                n.Draw(canvas, context);
            }
        }
    }
}
