using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.Network
{
    // Simple network node component; discoverable via SkiaComponentRegistry
    public class NetworkNode : SkiaComponent
    {
    public SKColor FillColor { get; set; } = MaterialDesignColors.Surface;
    public SKColor StrokeColor { get; set; } = MaterialDesignColors.Outline;
        public float StrokeWidth { get; set; } = 1.5f;
        public float CornerRadius { get; set; } = 6f;

        // Additional properties for advanced network functionality
        public string NodeType { get; set; } = "Default";
    public bool IsHighlighted { get; set; } = false;
        public float Scale { get; set; } = 1.0f;
        public SKColor CentralityColor { get; set; } = SKColors.Transparent;
        public SKColor CommunityColor { get; set; } = SKColors.Transparent;
        public int CommunityId { get; set; } = 0;

        // Ports configuration
        private const float PortRadius = 5f;
        private const float PortPadding = 8f;
        private int _inputPortCount = 1;
        private int _outputPortCount = 1;

        public int InputPortCount
        {
            get => _inputPortCount;
            set
            {
                var v = Math.Max(0, value);
                if (_inputPortCount != v)
                {
                    _inputPortCount = v;
                    EnsurePortCounts(_inputPortCount, _outputPortCount);
                }
            }
        }

        public int OutputPortCount
        {
            get => _outputPortCount;
            set
            {
                var v = Math.Max(0, value);
                if (_outputPortCount != v)
                {
                    _outputPortCount = v;
                    EnsurePortCounts(_inputPortCount, _outputPortCount);
                }
            }
        }

        public NetworkNode()
        {
            Width = 100;
            Height = 48;
            Name = "Node";
            // Default one-in/one-out
            EnsurePortCounts(_inputPortCount, _outputPortCount);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Apply scaling
            float scaledWidth = Width * Scale;
            float scaledHeight = Height * Scale;
            float scaledX = X - (scaledWidth - Width) / 2;
            float scaledY = Y - (scaledHeight - Height) / 2;

            // Determine effective colors
            SKColor effectiveFillColor = IsHighlighted ? MaterialDesignColors.TertiaryContainer : FillColor;
            if (CommunityColor != SKColors.Transparent)
            {
                effectiveFillColor = CommunityColor;
            }
            else if (CentralityColor != SKColors.Transparent)
            {
                effectiveFillColor = CentralityColor;
            }

            using var fill = new SKPaint { Color = effectiveFillColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = StrokeColor, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true };
            var rect = new SKRect(scaledX, scaledY, scaledX + scaledWidth, scaledY + scaledHeight);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            // label
            using var text = new SKPaint { Color = MaterialDesignColors.OnSurface, IsAntialias = true };
            using var font = new SKFont { Size = 14 * Scale };
            var label = string.IsNullOrWhiteSpace(Name) ? "Node" : Name;
            var tb = new SKRect();
            font.MeasureText(label, out tb);
            float tx = scaledX + (scaledWidth - tb.Width) / 2f;
            float ty = scaledY + (scaledHeight + tb.Height) / 2f - 3f;
            canvas.DrawText(label, tx, ty, SKTextAlign.Left, font, text);

            // draw ports
            DrawPorts(canvas, scaledX, scaledY, scaledWidth, scaledHeight);
        }

        private void DrawPorts(SKCanvas canvas, float scaledX, float scaledY, float scaledWidth, float scaledHeight)
        {
            using var inFill = new SKPaint { Color = MaterialDesignColors.SecondaryContainer, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var outFill = new SKPaint { Color = MaterialDesignColors.Primary, Style = SKPaintStyle.Fill, IsAntialias = true };
            foreach (var p in InConnectionPoints)
                canvas.DrawCircle(p.Center, PortRadius, inFill);
            foreach (var p in OutConnectionPoints)
                canvas.DrawCircle(p.Center, PortRadius, outFill);
        }

    private void EnsurePortCounts(int inCount, int outCount)
        {
            // inputs
            while (InConnectionPoints.Count < inCount)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, DataType = "object", IsAvailable = true });
            while (InConnectionPoints.Count > inCount)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            // outputs
            while (OutConnectionPoints.Count < outCount)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, DataType = "object", IsAvailable = true });
            while (OutConnectionPoints.Count > outCount)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            LayoutPorts();
        }

        private void LayoutPorts()
        {
            // Use scaled rectangle for visual alignment
            float scaledWidth = Width * Scale;
            float scaledHeight = Height * Scale;
            float scaledX = X - (scaledWidth - Width) / 2;
            float scaledY = Y - (scaledHeight - Height) / 2;

            float inTop = scaledY + PortPadding;
            float inBottom = scaledY + scaledHeight - PortPadding;
            float outTop = inTop;
            float outBottom = inBottom;

            PositionPortsAlongEdge(InConnectionPoints, scaledX, inTop, inBottom, -1);
            PositionPortsAlongEdge(OutConnectionPoints, scaledX + scaledWidth, outTop, outBottom, +1);
        }

        private void PositionPortsAlongEdge(System.Collections.Generic.List<IConnectionPoint> ports, float edgeX, float top, float bottom, int dir)
        {
            int n = Math.Max(ports.Count, 1);
            float span = Math.Max(bottom - top, 1f);
            for (int i = 0; i < ports.Count; i++)
            {
                var p = ports[i];
                float t = (i + 1) / (float)(n + 1);
                float cy = top + t * span;
                float cx = edgeX + dir * (PortRadius + 2);
                p.Center = new SKPoint(cx, cy);
                p.Position = p.Center;
                float r = PortRadius;
                p.Bounds = new SKRect(cx - r, cy - r, cx + r, cy + r);
                p.Rect = p.Bounds;
                p.Index = i;
                p.Component = this;
                p.IsAvailable = true;
            }
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            LayoutPorts();
            base.OnBoundsChanged(bounds);
        }
    }
}
