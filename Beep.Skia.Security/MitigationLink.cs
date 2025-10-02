using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum LinkStyle { Solid, Dashed }

    // A simple visual link representing mitigation mapping (Threat -> Control/Policy)
    public class MitigationLink : SecurityControl
    {
        private float _thickness = 2f;
        private SKColor _color = new SKColor(0, 122, 204);
        private LinkStyle _style = LinkStyle.Solid;

        public float Thickness { get => _thickness; set { var v = Math.Max(0.5f, Math.Min(10f, value)); if (Math.Abs(_thickness - v) > float.Epsilon) { _thickness = v; if (NodeProperties.TryGetValue("Thickness", out var p)) p.ParameterCurrentValue = _thickness; else NodeProperties["Thickness"] = new ParameterInfo { ParameterName = "Thickness", ParameterType = typeof(float), DefaultParameterValue = _thickness, ParameterCurrentValue = _thickness, Description = "Line thickness" }; InvalidateVisual(); } } }
        public SKColor Color { get => _color; set { if (_color != value) { _color = value; if (NodeProperties.TryGetValue("Color", out var p)) p.ParameterCurrentValue = _color; else NodeProperties["Color"] = new ParameterInfo { ParameterName = "Color", ParameterType = typeof(SKColor), DefaultParameterValue = _color, ParameterCurrentValue = _color, Description = "Line color" }; InvalidateVisual(); } } }
        public LinkStyle Style { get => _style; set { if (_style != value) { _style = value; if (NodeProperties.TryGetValue("Style", out var p)) p.ParameterCurrentValue = _style; else NodeProperties["Style"] = new ParameterInfo { ParameterName = "Style", ParameterType = typeof(LinkStyle), DefaultParameterValue = _style, ParameterCurrentValue = _style, Description = "Link style", Choices = Enum.GetNames(typeof(LinkStyle)) }; InvalidateVisual(); } } }

        public MitigationLink()
        {
            Width = 100; Height = 30; // nominal
            EnsurePortCounts(0, 0);
            NodeProperties["Thickness"] = new ParameterInfo { ParameterName = "Thickness", ParameterType = typeof(float), DefaultParameterValue = _thickness, ParameterCurrentValue = _thickness, Description = "Line thickness" };
            NodeProperties["Color"] = new ParameterInfo { ParameterName = "Color", ParameterType = typeof(SKColor), DefaultParameterValue = _color, ParameterCurrentValue = _color, Description = "Line color" };
            NodeProperties["Style"] = new ParameterInfo { ParameterName = "Style", ParameterType = typeof(LinkStyle), DefaultParameterValue = _style, ParameterCurrentValue = _style, Description = "Link style", Choices = Enum.GetNames(typeof(LinkStyle)) };
        }

        protected override void DrawSecurityContent(SKCanvas canvas, DrawingContext context)
        {
            using var paint = new SKPaint { Color = Color, StrokeWidth = Thickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            if (Style == LinkStyle.Dashed)
            {
                paint.PathEffect = SKPathEffect.CreateDash(new float[] { 6, 4 }, 0);
            }

            var y = Y + Height / 2f;
            canvas.DrawLine(X, y, X + Width, y, paint);
        }
    }
}
