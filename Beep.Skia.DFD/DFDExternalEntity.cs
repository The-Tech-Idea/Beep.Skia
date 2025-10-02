using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD External Entity: rectangle node.
    /// </summary>
    public class DFDExternalEntity : DFDControl
    {
        private string _label = "External";
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
        public DFDExternalEntity()
        {
            Name = "External";
            DisplayText = "External";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text label shown with the external entity."
            };
        }

        protected override void LayoutPorts()
        {
            // Simple rectangle: keep ports off the exact corners
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            DrawPorts(canvas);
        }
    }
}
