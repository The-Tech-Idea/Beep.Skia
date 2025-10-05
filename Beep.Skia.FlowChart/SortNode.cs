using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Sort node: two overlapping triangles for sorting operations.
    /// </summary>
    public class SortNode : FlowchartControl
    {
        private string _label = "Sort";
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

        private string _sortKey = "";
        public string SortKey
        {
            get => _sortKey;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_sortKey, v, System.StringComparison.Ordinal))
                {
                    _sortKey = v;
                    if (NodeProperties.TryGetValue("SortKey", out var pi))
                        pi.ParameterCurrentValue = _sortKey;
                    InvalidateVisual();
                }
            }
        }

        public SortNode()
        {
            Name = "Flowchart Sort";
            Width = 120;
            Height = 100;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Sort operation description."
            };
            NodeProperties["SortKey"] = new ParameterInfo
            {
                ParameterName = "SortKey",
                ParameterType = typeof(string),
                DefaultParameterValue = _sortKey,
                ParameterCurrentValue = _sortKey,
                Description = "Field or key used for sorting."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xED, 0xE7, 0xF6), IsAntialias = true }; // Light deep purple
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x67, 0x3A, 0xB7), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Deep purple
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Two overlapping triangles (diamond shape)
            float midY = r.MidY;
            float offset = r.Height * 0.15f;

            // Upper triangle (pointing down)
            using var upperPath = new SKPath();
            upperPath.MoveTo(r.MidX, r.Top);
            upperPath.LineTo(r.Right, midY - offset);
            upperPath.LineTo(r.Left, midY - offset);
            upperPath.Close();

            // Lower triangle (pointing up)
            using var lowerPath = new SKPath();
            lowerPath.MoveTo(r.MidX, r.Bottom);
            lowerPath.LineTo(r.Left, midY + offset);
            lowerPath.LineTo(r.Right, midY + offset);
            lowerPath.Close();

            canvas.DrawPath(upperPath, fill);
            canvas.DrawPath(upperPath, stroke);
            canvas.DrawPath(lowerPath, fill);
            canvas.DrawPath(lowerPath, stroke);

            // Draw label
            float labelY = r.MidY;
            if (!string.IsNullOrWhiteSpace(SortKey))
            {
                labelY = r.MidY - 5;
            }

            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            canvas.DrawText(Label, tx, labelY, SKTextAlign.Left, font, text);

            // Draw sort key if provided
            if (!string.IsNullOrWhiteSpace(SortKey))
            {
                using var smallFont = new SKFont(SKTypeface.Default, 10);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                float keyWidth = smallFont.MeasureText(SortKey, grayText);
                canvas.DrawText(SortKey, r.MidX - keyWidth / 2, r.MidY + 10, SKTextAlign.Left, smallFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
