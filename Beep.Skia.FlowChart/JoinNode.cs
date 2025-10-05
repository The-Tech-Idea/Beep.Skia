using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Join node (parallel synchronization): thick vertical bar with multiple inputs and single output.
    /// Represents waiting for all parallel paths to complete before continuing.
    /// </summary>
    public class JoinNode : FlowchartControl
    {
        private int _waitCount = 2;
        public int WaitCount
        {
            get => _waitCount;
            set
            {
                int v = System.Math.Max(2, System.Math.Min(8, value));
                if (_waitCount != v)
                {
                    _waitCount = v;
                    if (NodeProperties.TryGetValue("WaitCount", out var pi))
                        pi.ParameterCurrentValue = _waitCount;
                    EnsurePortCounts(_waitCount, 1);
                    InvalidateVisual();
                }
            }
        }

        public JoinNode()
        {
            Name = "Flowchart Join";
            Width = 120;
            Height = 100;
            EnsurePortCounts(2, 1);

            NodeProperties["WaitCount"] = new ParameterInfo
            {
                ParameterName = "WaitCount",
                ParameterType = typeof(int),
                DefaultParameterValue = _waitCount,
                ParameterCurrentValue = _waitCount,
                Description = "Number of parallel paths to wait for (2-8)."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Multiple input ports: distributed on left side
            if (InConnectionPoints.Count > 0)
            {
                PlacePortsAlongVerticalEdge(InConnectionPoints, r.Left, r.Top + 20f, r.Bottom - 20f, outwardSign: -1f);
            }

            // Single output port: bottom center
            if (OutConnectionPoints.Count > 0)
            {
                var outPt = OutConnectionPoints[0];
                outPt.Center = new SKPoint(r.MidX, r.Bottom);
                outPt.Position = new SKPoint(r.MidX, r.Bottom + PortRadius);
                outPt.Bounds = new SKRect(
                    outPt.Center.X - PortRadius,
                    outPt.Center.Y - PortRadius,
                    outPt.Center.X + PortRadius,
                    outPt.Center.Y + PortRadius
                );
                outPt.Rect = outPt.Bounds;
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float barThickness = 8f;
            float barHeight = r.Height * 0.6f;
            float barY = r.Top + 20f;

            // Thick vertical bar
            var barRect = new SKRect(
                r.MidX - barThickness / 2,
                barY,
                r.MidX + barThickness / 2,
                barY + barHeight
            );

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0x42, 0x42, 0x42), IsAntialias = true }; // Dark gray
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 10);

            canvas.DrawRect(barRect, fill);
            canvas.DrawRect(barRect, stroke);

            // Draw "JOIN" label above bar
            string label = "JOIN";
            float labelWidth = font.MeasureText(label, text);
            canvas.DrawText(label, r.MidX - labelWidth / 2, r.Top + 12, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
