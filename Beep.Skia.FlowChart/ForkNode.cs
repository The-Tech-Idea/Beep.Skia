using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Fork node (parallel split): thick vertical bar with single input and multiple parallel outputs.
    /// Represents splitting flow into concurrent/parallel paths.
    /// </summary>
    public class ForkNode : FlowchartControl
    {
        private int _parallelPaths = 2;
        public int ParallelPaths
        {
            get => _parallelPaths;
            set
            {
                int v = System.Math.Max(2, System.Math.Min(8, value));
                if (_parallelPaths != v)
                {
                    _parallelPaths = v;
                    if (NodeProperties.TryGetValue("ParallelPaths", out var pi))
                        pi.ParameterCurrentValue = _parallelPaths;
                    EnsurePortCounts(1, _parallelPaths);
                    InvalidateVisual();
                }
            }
        }

        public ForkNode()
        {
            Name = "Flowchart Fork";
            Width = 120;
            Height = 100;
            EnsurePortCounts(1, 2);

            NodeProperties["ParallelPaths"] = new ParameterInfo
            {
                ParameterName = "ParallelPaths",
                ParameterType = typeof(int),
                DefaultParameterValue = _parallelPaths,
                ParameterCurrentValue = _parallelPaths,
                Description = "Number of parallel output paths (2-8)."
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Single input port: top center
            if (InConnectionPoints.Count > 0)
            {
                var inPt = InConnectionPoints[0];
                inPt.Center = new SKPoint(r.MidX, r.Top);
                inPt.Position = new SKPoint(r.MidX, r.Top - PortRadius);
                inPt.Bounds = new SKRect(
                    inPt.Center.X - PortRadius,
                    inPt.Center.Y - PortRadius,
                    inPt.Center.X + PortRadius,
                    inPt.Center.Y + PortRadius
                );
                inPt.Rect = inPt.Bounds;
            }

            // Multiple output ports: distributed on right side
            if (OutConnectionPoints.Count > 0)
            {
                PlacePortsAlongVerticalEdge(OutConnectionPoints, r.Right, r.Top + 20f, r.Bottom - 20f, outwardSign: +1f);
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float barThickness = 8f;
            float barHeight = r.Height * 0.6f;
            float barY = r.MidY - barHeight / 2;

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

            // Draw "FORK" label below bar
            string label = "FORK";
            float labelWidth = font.MeasureText(label, text);
            canvas.DrawText(label, r.MidX - labelWidth / 2, r.Bottom - 8, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
