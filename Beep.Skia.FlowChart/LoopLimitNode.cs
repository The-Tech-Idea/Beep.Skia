using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Loop Limit node: hexagon with horizontal orientation for loop boundaries.
    /// Used to mark the beginning or end of a loop structure.
    /// </summary>
    public class LoopLimitNode : FlowchartControl
    {
        private string _label = "Loop Limit";
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v, System.StringComparison.Ordinal))
                {
                    _label = v;
                    if (NodeProperties.TryGetValue("Label", out var pi))
                        pi.ParameterCurrentValue = _label;
                    InvalidateVisual();
                }
            }
        }

        private LoopLimitType _limitType = LoopLimitType.Begin;
        /// <summary>
        /// Gets or sets the limit type.
        /// </summary>
        public LoopLimitType LimitType
        {
            get => _limitType;
            set
            {
                if (_limitType != value)
                {
                    _limitType = value;
                    if (NodeProperties.TryGetValue("LimitType", out var pi))
                        pi.ParameterCurrentValue = _limitType;
                    InvalidateVisual();
                }
            }
        }

        public enum LoopLimitType
        {
            Begin,
            End
        }

        /// <summary>
        /// Loop limit description.
        /// </summary>
        public LoopLimitNode()
        {
            Name = "Flowchart Loop Limit";
            Width = 140;
            Height = 60;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Loop limit description."
            };
            NodeProperties["LimitType"] = new ParameterInfo
            {
                ParameterName = "LimitType",
                ParameterType = typeof(LoopLimitType),
                DefaultParameterValue = _limitType,
                ParameterCurrentValue = _limitType,
                Description = "Whether this marks loop begin or end.",
                Choices = new[] { "Begin", "End" }
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float indent = r.Width * 0.15f;

            // Horizontal hexagon (same shape as PreparationNode)
            var points = new SKPoint[]
            {
                new SKPoint(r.Left + indent, r.Top),
                new SKPoint(r.Right - indent, r.Top),
                new SKPoint(r.Right, r.MidY),
                new SKPoint(r.Right - indent, r.Bottom),
                new SKPoint(r.Left + indent, r.Bottom),
                new SKPoint(r.Left, r.MidY)
            };

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true }; // Light orange
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xFF, 0x98, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Orange
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var pathBuilder = new SKPathBuilder();

            pathBuilder.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                pathBuilder.LineTo(points[i]);
            pathBuilder.Close();

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label with limit type
            string displayText = $"{LimitType}: {Label}";
            var tx = r.MidX - font.MeasureText(displayText, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(displayText, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
