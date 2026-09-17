using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a document in a business process diagram.
    /// Displayed as a rectangle with a folded corner.
    /// </summary>
    public class Document : BusinessControl
    {
        private string _label = "Document";
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

        public Document()
        {
            Width = 100;
            Height = 80;
            Name = _label;
            ComponentType = BusinessComponentType.Document;
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

            // Create document path with folded corner
            using var pathBuilder = new SKPathBuilder();
            float foldSize = 15;

            pathBuilder.MoveTo(X, Y);
            pathBuilder.LineTo(X + Width - foldSize, Y);
            pathBuilder.LineTo(X + Width, Y + foldSize);
            pathBuilder.LineTo(X + Width, Y + Height);
            pathBuilder.LineTo(X, Y + Height);
            pathBuilder.Close();

            // Draw main document
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Draw fold line
            using var foldPathBuilder = new SKPathBuilder();
            foldPathBuilder.MoveTo(X + Width - foldSize, Y);
            foldPathBuilder.LineTo(X + Width - foldSize, Y + foldSize);
            foldPathBuilder.LineTo(X + Width, Y + foldSize);

            using var foldPath = foldPathBuilder.Detach();
            canvas.DrawPath(foldPath, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Rectangle with folded corner: vertical segments, 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}