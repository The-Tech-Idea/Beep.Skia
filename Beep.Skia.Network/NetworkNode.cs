using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.Network
{
    // Simple network node component; discoverable via SkiaComponentRegistry
    public class NetworkNode : MaterialControl
    {
        private SKColor _fillColor = MaterialDesignColors.Surface;
        public SKColor FillColor { get => _fillColor; set { if (_fillColor == value) return; _fillColor = value; if (NodeProperties.TryGetValue("FillColor", out var pi)) pi.ParameterCurrentValue = _fillColor; InvalidateVisual(); } }
        private SKColor _strokeColor = MaterialDesignColors.Outline;
        public SKColor StrokeColor { get => _strokeColor; set { if (_strokeColor == value) return; _strokeColor = value; if (NodeProperties.TryGetValue("StrokeColor", out var pi)) pi.ParameterCurrentValue = _strokeColor; InvalidateVisual(); } }
        private float _strokeWidth = 1.5f;
        public float StrokeWidth { get => _strokeWidth; set { if (System.Math.Abs(_strokeWidth - value) < 0.0001f) return; _strokeWidth = value; if (NodeProperties.TryGetValue("StrokeWidth", out var pi)) pi.ParameterCurrentValue = _strokeWidth; InvalidateVisual(); } }
        private float _cornerRadius = 6f;
        public float CornerRadius { get => _cornerRadius; set { if (System.Math.Abs(_cornerRadius - value) < 0.0001f) return; _cornerRadius = value; if (NodeProperties.TryGetValue("CornerRadius", out var pi)) pi.ParameterCurrentValue = _cornerRadius; InvalidateVisual(); } }

        // Additional properties for advanced network functionality
        private string _nodeType = "Default";
        public string NodeType { get => _nodeType; set { if (_nodeType == value) return; _nodeType = value ?? string.Empty; if (NodeProperties.TryGetValue("NodeType", out var pi)) pi.ParameterCurrentValue = _nodeType; InvalidateVisual(); } }
        private bool _isHighlighted = false;
        public bool IsHighlighted { get => _isHighlighted; set { if (_isHighlighted == value) return; _isHighlighted = value; if (NodeProperties.TryGetValue("IsHighlighted", out var pi)) pi.ParameterCurrentValue = _isHighlighted; InvalidateVisual(); } }
        private float _scale = 1.0f;
        public float Scale { get => _scale; set { if (System.Math.Abs(_scale - value) < 0.0001f) return; _scale = value; if (NodeProperties.TryGetValue("Scale", out var pi)) pi.ParameterCurrentValue = _scale; MarkPortsDirty(); InvalidateVisual(); } }
        private SKColor _centralityColor = SKColors.Transparent;
        public SKColor CentralityColor { get => _centralityColor; set { if (_centralityColor == value) return; _centralityColor = value; if (NodeProperties.TryGetValue("CentralityColor", out var pi)) pi.ParameterCurrentValue = _centralityColor; InvalidateVisual(); } }
        private SKColor _communityColor = SKColors.Transparent;
        public SKColor CommunityColor { get => _communityColor; set { if (_communityColor == value) return; _communityColor = value; if (NodeProperties.TryGetValue("CommunityColor", out var pi)) pi.ParameterCurrentValue = _communityColor; InvalidateVisual(); } }
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
                    if (NodeProperties.TryGetValue("InputPortCount", out var pi)) pi.ParameterCurrentValue = _inputPortCount;
                    InvalidateVisual();
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
                    if (NodeProperties.TryGetValue("OutputPortCount", out var pi)) pi.ParameterCurrentValue = _outputPortCount;
                    InvalidateVisual();
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

            // Seed NodeProperties
            NodeProperties["FillColor"] = new ParameterInfo { ParameterName = "FillColor", ParameterType = typeof(SKColor), DefaultParameterValue = _fillColor, ParameterCurrentValue = _fillColor, Description = "Node fill color" };
            NodeProperties["StrokeColor"] = new ParameterInfo { ParameterName = "StrokeColor", ParameterType = typeof(SKColor), DefaultParameterValue = _strokeColor, ParameterCurrentValue = _strokeColor, Description = "Node border color" };
            NodeProperties["StrokeWidth"] = new ParameterInfo { ParameterName = "StrokeWidth", ParameterType = typeof(float), DefaultParameterValue = _strokeWidth, ParameterCurrentValue = _strokeWidth, Description = "Border thickness" };
            NodeProperties["CornerRadius"] = new ParameterInfo { ParameterName = "CornerRadius", ParameterType = typeof(float), DefaultParameterValue = _cornerRadius, ParameterCurrentValue = _cornerRadius, Description = "Corner radius" };
            NodeProperties["NodeType"] = new ParameterInfo { ParameterName = "NodeType", ParameterType = typeof(string), DefaultParameterValue = _nodeType, ParameterCurrentValue = _nodeType, Description = "Classification of node" };
            NodeProperties["IsHighlighted"] = new ParameterInfo { ParameterName = "IsHighlighted", ParameterType = typeof(bool), DefaultParameterValue = _isHighlighted, ParameterCurrentValue = _isHighlighted, Description = "Highlight state" };
            NodeProperties["Scale"] = new ParameterInfo { ParameterName = "Scale", ParameterType = typeof(float), DefaultParameterValue = _scale, ParameterCurrentValue = _scale, Description = "Visual scale factor" };
            NodeProperties["CentralityColor"] = new ParameterInfo { ParameterName = "CentralityColor", ParameterType = typeof(SKColor), DefaultParameterValue = _centralityColor, ParameterCurrentValue = _centralityColor, Description = "Centrality-based color" };
            NodeProperties["CommunityColor"] = new ParameterInfo { ParameterName = "CommunityColor", ParameterType = typeof(SKColor), DefaultParameterValue = _communityColor, ParameterCurrentValue = _communityColor, Description = "Community color" };
            NodeProperties["InputPortCount"] = new ParameterInfo { ParameterName = "InputPortCount", ParameterType = typeof(int), DefaultParameterValue = _inputPortCount, ParameterCurrentValue = _inputPortCount, Description = "Number of input ports" };
            NodeProperties["OutputPortCount"] = new ParameterInfo { ParameterName = "OutputPortCount", ParameterType = typeof(int), DefaultParameterValue = _outputPortCount, ParameterCurrentValue = _outputPortCount, Description = "Number of output ports" };
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Ensure ports layout lazily, then delegate to visuals
            EnsurePortLayout(() => LayoutPorts());
            DrawNetworkContent(canvas, context);
        }

        /// <summary>
        /// Template method for drawing network visuals. Ports are laid out by the base wrapper.
        /// </summary>
        protected virtual void DrawNetworkContent(SKCanvas canvas, DrawingContext context)
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

            // mark for lazy layout on next draw
            MarkPortsDirty();
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
            // base marks ports dirty; layout will occur lazily on next draw
            base.OnBoundsChanged(bounds);
        }
    }
}
