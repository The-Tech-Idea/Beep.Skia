using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a BPMN 2.0 gateway with configurable type.
    /// Exclusive (XOR) = blank diamond with X; Inclusive (OR) = diamond with circle; Parallel (AND) = diamond with +.
    /// </summary>
    public class Gateway : BusinessControl
    {
        private string _label = "Gateway";
        private GatewayType _gatewayType = GatewayType.Exclusive;

        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value ?? string.Empty;
                    Name = _label;
                    if (NodeProperties.TryGetValue("Label", out var p)) p.ParameterCurrentValue = _label; else NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Display label" };
                    InvalidateVisual();
                }
            }
        }

        public GatewayType GatewayType
        {
            get => _gatewayType;
            set
            {
                if (_gatewayType != value)
                {
                    _gatewayType = value;
                    if (NodeProperties.TryGetValue("GatewayType", out var p)) p.ParameterCurrentValue = _gatewayType; else NodeProperties["GatewayType"] = new ParameterInfo { ParameterName = "GatewayType", ParameterType = typeof(GatewayType), DefaultParameterValue = _gatewayType, ParameterCurrentValue = _gatewayType, Description = "Gateway type", Choices = Enum.GetNames(typeof(GatewayType)) };
                    InvalidateVisual();
                }
            }
        }

        public Gateway()
        {
            Width = 70;
            Height = 70;
            Name = _label;
            ComponentType = BusinessComponentType.Gateway;
            NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Display label" };
            NodeProperties["GatewayType"] = new ParameterInfo { ParameterName = "GatewayType", ParameterType = typeof(GatewayType), DefaultParameterValue = _gatewayType, ParameterCurrentValue = _gatewayType, Description = "Gateway type", Choices = Enum.GetNames(typeof(GatewayType)) };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;

            using var path = new SKPath();
            path.MoveTo(centerX, Y + 5);
            path.LineTo(X + Width - 5, centerY);
            path.LineTo(centerX, Y + Height - 5);
            path.LineTo(X + 5, centerY);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Draw gateway-specific icon
            DrawGatewayIcon(canvas, centerX, centerY);
        }

        private void DrawGatewayIcon(SKCanvas canvas, float cx, float cy)
        {
            using var iconPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            using var iconFill = new SKPaint
            {
                Color = BorderColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            float s = 11f;

            switch (_gatewayType)
            {
                case GatewayType.Exclusive:
                    canvas.DrawLine(cx - s, cy - s, cx + s, cy + s, iconPaint);
                    canvas.DrawLine(cx + s, cy - s, cx - s, cy + s, iconPaint);
                    break;

                case GatewayType.Inclusive:
                    canvas.DrawCircle(cx, cy, s * 0.7f, iconPaint);
                    break;

                case GatewayType.Parallel:
                    canvas.DrawLine(cx - s, cy, cx + s, cy, iconPaint);
                    canvas.DrawLine(cx, cy - s, cx, cy + s, iconPaint);
                    break;

                case GatewayType.Complex:
                    canvas.DrawLine(cx - s, cy, cx + s, cy, iconPaint);
                    canvas.DrawLine(cx, cy - s, cx, cy + s, iconPaint);
                    canvas.DrawLine(cx - s, cy - s, cx + s, cy + s, iconPaint);
                    canvas.DrawLine(cx + s, cy - s, cx - s, cy + s, iconPaint);
                    break;

                case GatewayType.EventBased:
                    float pr = s * 0.5f;
                    var pentPath = new SKPath();
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = (float)(-Math.PI / 2 + 2 * Math.PI * i / 5);
                        float px = cx + pr * (float)Math.Cos(angle);
                        float py = cy + pr * (float)Math.Sin(angle);
                        if (i == 0) pentPath.MoveTo(px, py);
                        else pentPath.LineTo(px, py);
                    }
                    pentPath.Close();
                    canvas.DrawPath(pentPath, iconPaint);
                    canvas.DrawCircle(cx, cy, pr * 0.3f, iconPaint);
                    break;
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            using var font = new SKFont(SKTypeface.Default, 10);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 15;

            canvas.DrawText(Name, centerX, textY, SKTextAlign.Center, font, paint);
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(1, 2);
            LayoutPortsVerticalSegments(topInset: Height * 0.25f, bottomInset: Height * 0.25f);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["GatewayType"] = _gatewayType;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("GatewayType", out var gt) && gt is GatewayType gtype) GatewayType = gtype;
        }
    }
}