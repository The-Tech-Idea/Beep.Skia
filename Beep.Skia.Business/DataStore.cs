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
        /// <summary>
        /// Display label
        /// </summary>
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

        /// <summary>
        /// Display label
        /// </summary>
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
            using var pathBuilder = new SKPathBuilder();
            float depth = 10;

            // Front face
            pathBuilder.MoveTo(X, Y + depth);
            pathBuilder.LineTo(X + Width - depth, Y + depth);
            pathBuilder.LineTo(X + Width - depth, Y + Height);
            pathBuilder.LineTo(X, Y + Height);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, borderPaint);

            // Top face
            using var topPathBuilder = new SKPathBuilder();
            topPathBuilder.MoveTo(X, Y + depth);
            topPathBuilder.LineTo(X + depth, Y);
            topPathBuilder.LineTo(X + Width, Y);
            topPathBuilder.LineTo(X + Width - depth, Y + depth);
            topPathBuilder.Close();

            using var topPath = topPathBuilder.Detach();
            canvas.DrawPath(topPath, fillPaint);
            canvas.DrawPath(topPath, borderPaint);

            // Right face
            using var rightPathBuilder = new SKPathBuilder();
            rightPathBuilder.MoveTo(X + Width - depth, Y + depth);
            rightPathBuilder.LineTo(X + Width, Y);
            rightPathBuilder.LineTo(X + Width, Y + Height - depth);
            rightPathBuilder.LineTo(X + Width - depth, Y + Height);
            rightPathBuilder.Close();

            using var rightPath = rightPathBuilder.Detach();
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
