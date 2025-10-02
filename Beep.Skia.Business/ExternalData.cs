using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents external data in a business process diagram.
    /// Displayed as a parallelogram.
    /// </summary>
    public class ExternalData : BusinessControl
    {
        private string _label = "External Data";
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

        public ExternalData()
        {
            Width = 110;
            Height = 60;
            Name = _label;
            ComponentType = BusinessComponentType.ExternalData;
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
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Create parallelogram path
            using var path = new SKPath();
            float skew = 15;

            path.MoveTo(X + skew, Y);
            path.LineTo(X + Width, Y);
            path.LineTo(X + Width - skew, Y + Height);
            path.LineTo(X, Y + Height);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Parallelogram: use vertical segments; 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}