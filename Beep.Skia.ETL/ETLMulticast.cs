using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Multicast node: sends identical copy of data to multiple outputs.
    /// Used for parallel processing or feeding multiple destinations.
    /// </summary>
    public class ETLMulticast : ETLControl
    {
        private int _outputCount = 2;
        public int OutputCount
        {
            get => _outputCount;
            set
            {
                var v = Math.Max(2, Math.Min(8, value));
                if (_outputCount == v) return;
                _outputCount = v;
                EnsurePortCounts(1, _outputCount);
                if (NodeProperties.TryGetValue("OutputCount", out var p))
                    p.ParameterCurrentValue = _outputCount;
                InvalidateVisual();
            }
        }

        public ETLMulticast()
        {
            Title = "Multicast";
            Subtitle = "Copy to Multiple";
            EnsurePortCounts(1, 2);
            HeaderColor = MaterialColors.TertiaryContainer;

            NodeProperties["OutputCount"] = new ParameterInfo
            {
                ParameterName = "OutputCount",
                ParameterType = typeof(int),
                DefaultParameterValue = _outputCount,
                ParameterCurrentValue = _outputCount,
                Description = "Number of output copies (2-8)"
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);

            // Draw broadcast icon (single point radiating to multiple)
            var r = Bounds;
            float centerX = r.MidX;
            float centerY = r.Top + HeaderHeight + (r.Height - HeaderHeight) / 2;
            float radius = 6f;
            float rayLength = 20f;

            using var iconPaint = new SKPaint
            {
                Color = MaterialColors.OnSurface.WithAlpha(128),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // Center point
            canvas.DrawCircle(centerX, centerY, radius, iconPaint);

            // Radiating lines
            for (int i = 0; i < 3; i++)
            {
                float angle = (float)(Math.PI / 4 + i * Math.PI / 4);
                float endX = centerX + (float)Math.Cos(angle) * rayLength;
                float endY = centerY + (float)Math.Sin(angle) * rayLength;
                canvas.DrawLine(centerX, centerY, endX, endY, iconPaint);
            }
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var r = Bounds;
            using var path = new SKPath();

            // Triangle/cone shape - single input fans out to multiple outputs
            float bottomWidth = r.Width * 0.8f;
            float bottomOffset = (r.Width - bottomWidth) / 2;

            path.MoveTo(r.MidX, r.Top + HeaderHeight);
            path.LineTo(r.Left + bottomOffset, r.Bottom);
            path.LineTo(r.Right - bottomOffset, r.Bottom);
            path.Close();

            using var fill = new SKPaint { Color = Background, IsAntialias = true };
            using var stroke = new SKPaint { Color = Stroke, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Single input port at top
            if (InConnectionPoints.Count > 0)
            {
                var pt = InConnectionPoints[0];
                pt.Center = new SKPoint(r.MidX, r.Top + HeaderHeight);
                pt.Position = new SKPoint(r.MidX, r.Top + HeaderHeight - PortRadius);
                pt.Bounds = new SKRect(
                    pt.Center.X - PortRadius,
                    pt.Center.Y - PortRadius,
                    pt.Center.X + PortRadius,
                    pt.Center.Y + PortRadius
                );
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }

            // Multiple output ports along bottom edge
            if (OutConnectionPoints.Count > 0)
            {
                float spacing = r.Width * 0.8f / (_outputCount + 1);
                float startX = r.Left + (r.Width - r.Width * 0.8f) / 2;

                for (int i = 0; i < OutConnectionPoints.Count && i < _outputCount; i++)
                {
                    var pt = OutConnectionPoints[i];
                    float x = startX + spacing * (i + 1);
                    pt.Center = new SKPoint(x, r.Bottom);
                    pt.Position = new SKPoint(x, r.Bottom + PortRadius);
                    pt.Bounds = new SKRect(
                        pt.Center.X - PortRadius,
                        pt.Center.Y - PortRadius,
                        pt.Center.X + PortRadius,
                        pt.Center.Y + PortRadius
                    );
                    pt.Rect = pt.Bounds;
                    pt.Component = this;
                    pt.IsAvailable = true;
                }
            }
        }
    }
}
