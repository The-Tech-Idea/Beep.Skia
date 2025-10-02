using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Predefined Process: rectangle with vertical double-struck edges and centered label.
    /// </summary>
    public class PredefinedProcessNode : FlowchartControl
    {
        private string _label = "Predefined Process";
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

        public PredefinedProcessNode()
        {
            Name = "Flowchart Predefined Process";
            Width = 160;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the predefined process rectangle."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;
            // Avoid double-struck inner edges by increasing inset
            PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 10f, r.Bottom - 10f, outwardSign: -1f);
            PlacePortsAlongVerticalEdge(OutConnectionPoints, r.Right, r.Top + 10f, r.Bottom - 10f, outwardSign: +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE1, 0xF5, 0xFE), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x03, 0xA9, 0xF4), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Double-struck vertical edges
            using var edge = new SKPaint { Color = stroke.Color, IsAntialias = true, StrokeWidth = 2 };
            float inset = 8f;
            canvas.DrawLine(r.Left + inset, r.Top, r.Left + inset, r.Bottom, edge);
            canvas.DrawLine(r.Right - inset, r.Top, r.Right - inset, r.Bottom, edge);

            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
