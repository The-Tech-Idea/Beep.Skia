using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Manual operation node: trapezoid shape (top wider than bottom) for human-performed tasks.
    /// </summary>
    public class ManualOperationNode : FlowchartControl
    {
        private string _label = "Manual Operation";
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

        public ManualOperationNode()
        {
            Name = "Flowchart Manual Operation";
            Width = 160;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the trapezoid."
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
            float slant = r.Width * 0.15f; // 15% slant inward at bottom

            // Trapezoid: top-left → top-right → bottom-right-inner → bottom-left-inner
            var topLeft = new SKPoint(r.Left, r.Top);
            var topRight = new SKPoint(r.Right, r.Top);
            var bottomRight = new SKPoint(r.Right - slant, r.Bottom);
            var bottomLeft = new SKPoint(r.Left + slant, r.Bottom);

            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xEB, 0xEE), IsAntialias = true }; // Light pink
            using var stroke = new SKPaint { Color = new SKColor(0xE5, 0x39, 0x35), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Red
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);
            using var path = new SKPath();

            path.MoveTo(topLeft);
            path.LineTo(topRight);
            path.LineTo(bottomRight);
            path.LineTo(bottomLeft);
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
