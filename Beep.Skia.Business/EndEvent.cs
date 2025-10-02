using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents an end event in a business process diagram.
    /// Displayed as a thick-bordered circle.
    /// </summary>
    public class EndEvent : BusinessControl
    {
        private string _label = "End";
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

        public EndEvent()
        {
            Width = 60;
            Height = 60;
            Name = _label;
            ComponentType = BusinessComponentType.EndEvent;
            NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Display label" };
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
                Color = MaterialColors.Outline,
                StrokeWidth = 4, // Thick border for end event
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - 4;

            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Place the single input on the ellipse perimeter to the left; no outputs.
            EnsurePortCounts(1, 0);
            LayoutPortsOnEllipse(topInset: 4f, bottomInset: 4f, outwardOffset: 2f);
        }
    }
}