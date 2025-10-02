using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process: rounded rectangle node with one input and one output.
    /// </summary>
    public class DFDProcess : DFDControl
    {
        private string _label = "Process";
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
        public DFDProcess()
        {
            Name = "Process";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text label shown with the process."
            };
        }

        protected override void LayoutPorts()
        {
            // Avoid rounded corners by starting/ending at corner radius
            LayoutPortsVerticalSegments(topInset: CornerRadius, bottomInset: CornerRadius);
        }

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var rect = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            DrawPorts(canvas);
        }
    }
}
