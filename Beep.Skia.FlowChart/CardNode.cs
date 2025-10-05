using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Card node: rectangle with cut corner for punch card or card-based storage.
    /// Legacy flowchart symbol for card-based data processing.
    /// </summary>
    public class CardNode : FlowchartControl
    {
        private string _label = "Card";
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

        public CardNode()
        {
            Name = "Flowchart Card";
            Width = 140;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Card label or identifier."
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
            float cornerCut = 12f;

            // Rectangle with cut top-right corner
            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true }; // Light orange
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0xFF, 0x98, 0x00), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Orange
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(r.Left, r.Top);
            path.LineTo(r.Right - cornerCut, r.Top);
            path.LineTo(r.Right, r.Top + cornerCut);
            path.LineTo(r.Right, r.Bottom);
            path.LineTo(r.Left, r.Bottom);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
