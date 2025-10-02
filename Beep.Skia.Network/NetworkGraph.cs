using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;
using System.Collections.Generic;

namespace Beep.Skia.Network
{
    public class NetworkGraph : MaterialControl
    {
        private SKColor _background = MaterialDesignColors.Surface;
    public SKColor Background { get => _background; set { if (_background == value) return; _background = value; if (NodeProperties.TryGetValue("Background", out var pi)) pi.ParameterCurrentValue = _background; InvalidateVisual(); } }
        private SKColor _gridColor = MaterialDesignColors.SurfaceVariant;
    public SKColor GridColor { get => _gridColor; set { if (_gridColor == value) return; _gridColor = value; if (NodeProperties.TryGetValue("GridColor", out var pi)) pi.ParameterCurrentValue = _gridColor; InvalidateVisual(); } }
        private float _gridSpacing = 24f;
    public float GridSpacing { get => _gridSpacing; set { if (System.Math.Abs(_gridSpacing - value) < 0.0001f) return; _gridSpacing = value; if (NodeProperties.TryGetValue("GridSpacing", out var pi)) pi.ParameterCurrentValue = _gridSpacing; InvalidateVisual(); } }

        public List<NetworkNode> Nodes { get; } = new List<NetworkNode>();
        public List<NetworkLink> Links { get; } = new List<NetworkLink>();

        public NetworkGraph()
        {
            Width = 800;
            Height = 600;
            Name = "NetworkGraph";

            // Seed NodeProperties for editor
            NodeProperties["Background"] = new ParameterInfo { ParameterName = "Background", ParameterType = typeof(SKColor), DefaultParameterValue = _background, ParameterCurrentValue = _background, Description = "Canvas background color" };
            NodeProperties["GridColor"] = new ParameterInfo { ParameterName = "GridColor", ParameterType = typeof(SKColor), DefaultParameterValue = _gridColor, ParameterCurrentValue = _gridColor, Description = "Grid line color" };
            NodeProperties["GridSpacing"] = new ParameterInfo { ParameterName = "GridSpacing", ParameterType = typeof(float), DefaultParameterValue = _gridSpacing, ParameterCurrentValue = _gridSpacing, Description = "Grid spacing in pixels" };
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
