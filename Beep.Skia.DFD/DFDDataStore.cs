using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Data Store: rectangle with double vertical line on the left.
    /// </summary>
    public class DFDDataStore : DFDControl
    {
        private string _label = "Data Store";
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
        public DFDDataStore()
        {
            Name = "Data Store";
            DisplayText = "Data Store";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text label shown with the data store."
            };
        }

        protected override void LayoutPorts()
        {
            // Slight insets to avoid corners; move inputs further left to clear the double line
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f, leftOffset: -8f, rightOffset: 2f);
        }

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRect(r, fill);
            canvas.DrawRect(r, stroke);

            using var line = new SKPaint { Color = stroke.Color, IsAntialias = true, StrokeWidth = 1.5f };
            canvas.DrawLine(r.Left + 6, r.Top, r.Left + 6, r.Bottom, line);

            DrawPorts(canvas);
        }
    }
}
