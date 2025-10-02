using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.Network
{
    // Simple visual link; not using IConnectionLine yet for simplicity
    public class NetworkLink : MaterialControl
    {
        public SKPoint Start { get; set; }
        public SKPoint End { get; set; }

        private SKColor _color = MaterialDesignColors.Outline;
        public SKColor Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    if (NodeProperties.TryGetValue("Color", out var p)) p.ParameterCurrentValue = value; else NodeProperties["Color"] = new ParameterInfo { ParameterName = "Color", ParameterType = typeof(SKColor), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Link color" };
                    InvalidateVisual();
                }
            }
        }

        private float _thickness = 2f;
        public float Thickness
        {
            get => _thickness;
            set
            {
                if (Math.Abs(_thickness - value) > float.Epsilon)
                {
                    _thickness = value;
                    if (NodeProperties.TryGetValue("Thickness", out var p)) p.ParameterCurrentValue = value; else NodeProperties["Thickness"] = new ParameterInfo { ParameterName = "Thickness", ParameterType = typeof(float), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Stroke width" };
                    InvalidateVisual();
                }
            }
        }

        // Additional properties for advanced network functionality
        public NetworkNode SourceNode { get; set; }
        public NetworkNode TargetNode { get; set; }

        private double _weight = 1.0;
        public double Weight
        {
            get => _weight;
            set
            {
                if (Math.Abs(_weight - value) > double.Epsilon)
                {
                    _weight = value;
                    if (NodeProperties.TryGetValue("Weight", out var p)) p.ParameterCurrentValue = value; else NodeProperties["Weight"] = new ParameterInfo { ParameterName = "Weight", ParameterType = typeof(double), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Link weight" };
                    InvalidateVisual();
                }
            }
        }

        private string _linkType = "Default";
        public string LinkType
        {
            get => _linkType;
            set
            {
                if (!string.Equals(_linkType, value))
                {
                    _linkType = value ?? "Default";
                    if (NodeProperties.TryGetValue("LinkType", out var p)) p.ParameterCurrentValue = _linkType; else NodeProperties["LinkType"] = new ParameterInfo { ParameterName = "LinkType", ParameterType = typeof(string), DefaultParameterValue = _linkType, ParameterCurrentValue = _linkType, Description = "Link classification/type", Choices = new[] { "Default", "Dashed", "Directed", "Weighted" } };
                    InvalidateVisual();
                }
            }
        }

        private float _arrowSize = 10f;
        public float ArrowSize
        {
            get => _arrowSize;
            set
            {
                if (Math.Abs(_arrowSize - value) > float.Epsilon)
                {
                    _arrowSize = value;
                    if (NodeProperties.TryGetValue("ArrowSize", out var p)) p.ParameterCurrentValue = value; else NodeProperties["ArrowSize"] = new ParameterInfo { ParameterName = "ArrowSize", ParameterType = typeof(float), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Arrow head size for directed links" };
                    InvalidateVisual();
                }
            }
        }

        private bool _isHighlighted = false;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    if (NodeProperties.TryGetValue("IsHighlighted", out var p)) p.ParameterCurrentValue = value; else NodeProperties["IsHighlighted"] = new ParameterInfo { ParameterName = "IsHighlighted", ParameterType = typeof(bool), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Highlight state" };
                    InvalidateVisual();
                }
            }
        }

        // Optional label drawn near the midpoint of the curve
        private string _label = string.Empty;
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v))
                {
                    _label = v;
                    if (NodeProperties.TryGetValue("Label", out var p)) p.ParameterCurrentValue = v; else NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = v, ParameterCurrentValue = v, Description = "Link label" };
                    InvalidateVisual();
                }
            }
        }

        private SKColor _labelColor = MaterialDesignColors.OnSurfaceVariant;
        public SKColor LabelColor
        {
            get => _labelColor;
            set
            {
                if (_labelColor != value)
                {
                    _labelColor = value;
                    if (NodeProperties.TryGetValue("LabelColor", out var p)) p.ParameterCurrentValue = value; else NodeProperties["LabelColor"] = new ParameterInfo { ParameterName = "LabelColor", ParameterType = typeof(SKColor), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Label color" };
                    InvalidateVisual();
                }
            }
        }

        private float _labelTextSize = 10f;
        public float LabelTextSize
        {
            get => _labelTextSize;
            set
            {
                if (Math.Abs(_labelTextSize - value) > float.Epsilon)
                {
                    _labelTextSize = value;
                    if (NodeProperties.TryGetValue("LabelTextSize", out var p)) p.ParameterCurrentValue = value; else NodeProperties["LabelTextSize"] = new ParameterInfo { ParameterName = "LabelTextSize", ParameterType = typeof(float), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Label text size" };
                    InvalidateVisual();
                }
            }
        }

        // Curve shaping factor (0..1); default ~0.33 for pleasing curve
        private float _curvature = 0.33f;
        public float Curvature
        {
            get => _curvature;
            set
            {
                var v = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(_curvature - v) > float.Epsilon)
                {
                    _curvature = v;
                    if (NodeProperties.TryGetValue("Curvature", out var p)) p.ParameterCurrentValue = v; else NodeProperties["Curvature"] = new ParameterInfo { ParameterName = "Curvature", ParameterType = typeof(float), DefaultParameterValue = v, ParameterCurrentValue = v, Description = "Curve shaping factor (0..1)" };
                    InvalidateVisual();
                }
            }
        }

        // Draw arrowheads on both ends when Directed
        private bool _bidirectional = false;
        public bool Bidirectional
        {
            get => _bidirectional;
            set
            {
                if (_bidirectional != value)
                {
                    _bidirectional = value;
                    if (NodeProperties.TryGetValue("Bidirectional", out var p)) p.ParameterCurrentValue = value; else NodeProperties["Bidirectional"] = new ParameterInfo { ParameterName = "Bidirectional", ParameterType = typeof(bool), DefaultParameterValue = value, ParameterCurrentValue = value, Description = "Arrowheads at both ends (Directed only)" };
                    InvalidateVisual();
                }
            }
        }

        public NetworkLink()
        {
            Width = 0; Height = 0; // not used; draws by absolute coords
            Name = "Link";
            IsStatic = false; // links can be moved if desired

            // Seed NodeProperties for editor round-trip
            NodeProperties["Color"] = new ParameterInfo { ParameterName = "Color", ParameterType = typeof(SKColor), DefaultParameterValue = _color, ParameterCurrentValue = _color, Description = "Link color" };
            NodeProperties["Thickness"] = new ParameterInfo { ParameterName = "Thickness", ParameterType = typeof(float), DefaultParameterValue = _thickness, ParameterCurrentValue = _thickness, Description = "Stroke width" };
            NodeProperties["Weight"] = new ParameterInfo { ParameterName = "Weight", ParameterType = typeof(double), DefaultParameterValue = _weight, ParameterCurrentValue = _weight, Description = "Link weight" };
            NodeProperties["LinkType"] = new ParameterInfo { ParameterName = "LinkType", ParameterType = typeof(string), DefaultParameterValue = _linkType, ParameterCurrentValue = _linkType, Description = "Link classification/type", Choices = new[] { "Default", "Dashed", "Directed", "Weighted" } };
            NodeProperties["IsHighlighted"] = new ParameterInfo { ParameterName = "IsHighlighted", ParameterType = typeof(bool), DefaultParameterValue = _isHighlighted, ParameterCurrentValue = _isHighlighted, Description = "Highlight state" };
            NodeProperties["ArrowSize"] = new ParameterInfo { ParameterName = "ArrowSize", ParameterType = typeof(float), DefaultParameterValue = _arrowSize, ParameterCurrentValue = _arrowSize, Description = "Arrow head size for directed links" };
            NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Link label" };
            NodeProperties["LabelColor"] = new ParameterInfo { ParameterName = "LabelColor", ParameterType = typeof(SKColor), DefaultParameterValue = _labelColor, ParameterCurrentValue = _labelColor, Description = "Label color" };
            NodeProperties["LabelTextSize"] = new ParameterInfo { ParameterName = "LabelTextSize", ParameterType = typeof(float), DefaultParameterValue = _labelTextSize, ParameterCurrentValue = _labelTextSize, Description = "Label text size" };
            NodeProperties["Curvature"] = new ParameterInfo { ParameterName = "Curvature", ParameterType = typeof(float), DefaultParameterValue = _curvature, ParameterCurrentValue = _curvature, Description = "Curve shaping factor (0..1)" };
            NodeProperties["Bidirectional"] = new ParameterInfo { ParameterName = "Bidirectional", ParameterType = typeof(bool), DefaultParameterValue = _bidirectional, ParameterCurrentValue = _bidirectional, Description = "Arrowheads at both ends (Directed only)" };
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

            // Determine effective color and stroke width
            SKColor effectiveColor = IsHighlighted ? MaterialDesignColors.Tertiary : Color;
            float strokeWidth = Thickness;
            if (string.Equals(LinkType, "Weighted", System.StringComparison.OrdinalIgnoreCase))
            {
                // Scale stroke by weight with gentle clamp
                var factor = (float)System.Math.Clamp(Weight, 0.5, 5.0);
                strokeWidth = System.Math.Max(0.5f, Thickness * factor);
            }

            using var paint = new SKPaint { Color = effectiveColor, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth, IsAntialias = true };
            SKPathEffect dashEffect = null;
            if (string.Equals(LinkType, "Dashed", System.StringComparison.OrdinalIgnoreCase))
            {
                dashEffect = SKPathEffect.CreateDash(new float[] { 8f, 4f }, 0f);
                paint.PathEffect = dashEffect;
            }
            // Simple cubic curve for aesthetics
            var dx = (End.X - Start.X) * Curvature;
            var c1 = new SKPoint(Start.X + dx, Start.Y);
            var c2 = new SKPoint(End.X - dx, End.Y);
            using var path = new SKPath();
            path.MoveTo(Start);
            path.CubicTo(c1, c2, End);
            canvas.DrawPath(path, paint);

            dashEffect?.Dispose();

            // Draw arrow head for directed links
            bool isDirected = string.Equals(LinkType, "Directed", System.StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(LinkType) && LinkType.IndexOf("Directed", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (isDirected)
            {
                // Tangent at t=1 for cubic is End - c2
                var vx = End.X - c2.X;
                var vy = End.Y - c2.Y;
                var mag = (float)System.Math.Sqrt(vx * vx + vy * vy);
                if (mag > 0.001f)
                {
                    var dirX = vx / mag;
                    var dirY = vy / mag;
                    var perpX = -dirY;
                    var perpY = dirX;

                    var tip = End;
                    var baseCx = End.X - dirX * ArrowSize;
                    var baseCy = End.Y - dirY * ArrowSize;
                    var leftX = baseCx + perpX * (ArrowSize * 0.5f);
                    var leftY = baseCy + perpY * (ArrowSize * 0.5f);
                    var rightX = baseCx - perpX * (ArrowSize * 0.5f);
                    var rightY = baseCy - perpY * (ArrowSize * 0.5f);

                    using var fill = new SKPaint { Color = effectiveColor, Style = SKPaintStyle.Fill, IsAntialias = true };
                    using var arrow = new SKPath();
                    arrow.MoveTo(tip);
                    arrow.LineTo(leftX, leftY);
                    arrow.LineTo(rightX, rightY);
                    arrow.Close();
                    canvas.DrawPath(arrow, fill);
                }

                // Optional arrow at start as well when Bidirectional
                if (Bidirectional)
                {
                    // Tangent at t=0 for cubic is c1 - Start
                    var svx = c1.X - Start.X;
                    var svy = c1.Y - Start.Y;
                    var smag = (float)System.Math.Sqrt(svx * svx + svy * svy);
                    if (smag > 0.001f)
                    {
                        var dirX = svx / smag;
                        var dirY = svy / smag;
                        var perpX = -dirY;
                        var perpY = dirX;

                        var tip = Start;
                        var baseCx = Start.X + dirX * ArrowSize; // arrow faces away from start toward c1
                        var baseCy = Start.Y + dirY * ArrowSize;
                        var leftX = baseCx + perpX * (ArrowSize * 0.5f);
                        var leftY = baseCy + perpY * (ArrowSize * 0.5f);
                        var rightX = baseCx - perpX * (ArrowSize * 0.5f);
                        var rightY = baseCy - perpY * (ArrowSize * 0.5f);

                        using var fill2 = new SKPaint { Color = effectiveColor, Style = SKPaintStyle.Fill, IsAntialias = true };
                        using var arrow2 = new SKPath();
                        arrow2.MoveTo(tip);
                        arrow2.LineTo(leftX, leftY);
                        arrow2.LineTo(rightX, rightY);
                        arrow2.Close();
                        canvas.DrawPath(arrow2, fill2);
                    }
                }
            }

            // Draw label near the midpoint of the curve
            if (!string.IsNullOrEmpty(Label))
            {
                // Point at t=0.5 on cubic Bezier
                float t = 0.5f;
                float omt = 1 - t;
                var midX = omt * omt * omt * Start.X + 3 * omt * omt * t * c1.X + 3 * omt * t * t * c2.X + t * t * t * End.X;
                var midY = omt * omt * omt * Start.Y + 3 * omt * omt * t * c1.Y + 3 * omt * t * t * c2.Y + t * t * t * End.Y;
                using var labelPaint = new SKPaint { Color = LabelColor, IsAntialias = true };
                using var labelFont = new SKFont(SKTypeface.Default, LabelTextSize);
                canvas.DrawText(Label, midX, midY - 6, SKTextAlign.Center, labelFont, labelPaint);
            }
        }
    }
}
