using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a data store in a business process diagram.
    /// Displayed as an open box.
    /// </summary>
    public class DataStore : BusinessControl
    {
        private string _label = "Data Store";
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

        public DataStore()
        {
            Width = 90;
            Height = 70;
            Name = _label;
            ComponentType = BusinessComponentType.DataStore;
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

            // Create open box path
            using var path = new SKPath();
            float depth = 10;

            // Front face
            path.MoveTo(X, Y + depth);
            path.LineTo(X + Width - depth, Y + depth);
            path.LineTo(X + Width - depth, Y + Height);
            path.LineTo(X, Y + Height);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Top face
            using var topPath = new SKPath();
            topPath.MoveTo(X, Y + depth);
            topPath.LineTo(X + depth, Y);
            topPath.LineTo(X + Width, Y);
            topPath.LineTo(X + Width - depth, Y + depth);
            topPath.Close();

            canvas.DrawPath(topPath, fillPaint);
            canvas.DrawPath(topPath, borderPaint);

            // Right face
            using var rightPath = new SKPath();
            rightPath.MoveTo(X + Width - depth, Y + depth);
            rightPath.LineTo(X + Width, Y);
            rightPath.LineTo(X + Width, Y + Height - depth);
            rightPath.LineTo(X + Width - depth, Y + Height);
            rightPath.Close();

            canvas.DrawPath(rightPath, fillPaint);
            canvas.DrawPath(rightPath, borderPaint);
        }

        protected override void LayoutPorts()
        {
            // Box-like: use vertical segments, 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}