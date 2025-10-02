using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Data (Database) node: cylinder shape representing a data store.
    /// </summary>
    public class DataNode : FlowchartControl
    {
        private string _label = "Data";
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

        public DataNode()
        {
            Name = "Flowchart Data";
            Width = 160;
            Height = 90;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text shown inside the cylinder."
            };
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;
            float ry = 14f; // should match drawing
            // Avoid top/bottom curved areas; place along mid body
            PlacePortsAlongVerticalEdge(InConnectionPoints, b.Left, b.Top + ry + 4f, b.Bottom - ry - 4f, outwardSign: -1f);
            PlacePortsAlongVerticalEdge(OutConnectionPoints, b.Right, b.Top + ry + 4f, b.Bottom - ry - 4f, outwardSign: +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE8, 0xEA, 0xF6), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x3F, 0x51, 0xB5), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            float rx = (b.Width) / 2f;
            float ry = 14f;

            // Top ellipse
            var topRect = new SKRect(b.Left, b.Top, b.Right, b.Top + ry * 2);
            canvas.DrawOval(topRect, fill);
            canvas.DrawOval(topRect, stroke);

            // Body
            using var bodyFill = new SKPaint { Color = fill.Color, IsAntialias = true };
            canvas.DrawRect(new SKRect(b.Left, b.Top + ry, b.Right, b.Bottom - ry), bodyFill);

            // Bottom ellipse outline
            var bottomRect = new SKRect(b.Left, b.Bottom - ry * 2, b.Right, b.Bottom);
            using var bottomStroke = new SKPaint { Color = stroke.Color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            canvas.DrawArc(bottomRect, 0, 180, false, bottomStroke);
            using var bottomDash = new SKPaint { Color = stroke.Color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2, PathEffect = SKPathEffect.CreateDash(new float[] { 6, 6 }, 0) };
            canvas.DrawArc(bottomRect, 180, 180, false, bottomDash);

            // Side lines
            canvas.DrawLine(b.Left, b.Top + ry, b.Left, b.Bottom - ry, stroke);
            canvas.DrawLine(b.Right, b.Top + ry, b.Right, b.Bottom - ry, stroke);

            var tx = b.MidX - font.MeasureText(Label, text) / 2;
            var ty = b.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
