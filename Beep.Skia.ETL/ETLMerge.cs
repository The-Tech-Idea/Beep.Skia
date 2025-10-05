using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Merge node: combines multiple input streams into one output (UNION ALL).
    /// Different from Join - simply concatenates rows from multiple sources.
    /// </summary>
    public class ETLMerge : ETLControl
    {
        private int _inputCount = 2;
        public int InputCount
        {
            get => _inputCount;
            set
            {
                var v = Math.Max(2, Math.Min(8, value));
                if (_inputCount == v) return;
                _inputCount = v;
                EnsurePortCounts(_inputCount, 1);
                if (NodeProperties.TryGetValue("InputCount", out var p))
                    p.ParameterCurrentValue = _inputCount;
                InvalidateVisual();
            }
        }

        private bool _removeDuplicates = false;
        public bool RemoveDuplicates
        {
            get => _removeDuplicates;
            set
            {
                if (_removeDuplicates == value) return;
                _removeDuplicates = value;
                if (NodeProperties.TryGetValue("RemoveDuplicates", out var p))
                    p.ParameterCurrentValue = _removeDuplicates;
                InvalidateVisual();
            }
        }

        public ETLMerge()
        {
            Title = "Merge";
            Subtitle = "Union All";
            EnsurePortCounts(2, 1);
            HeaderColor = MaterialColors.TertiaryContainer;

            NodeProperties["InputCount"] = new ParameterInfo
            {
                ParameterName = "InputCount",
                ParameterType = typeof(int),
                DefaultParameterValue = _inputCount,
                ParameterCurrentValue = _inputCount,
                Description = "Number of input streams to merge (2-8)"
            };
            NodeProperties["RemoveDuplicates"] = new ParameterInfo
            {
                ParameterName = "RemoveDuplicates",
                ParameterType = typeof(bool),
                DefaultParameterValue = _removeDuplicates,
                ParameterCurrentValue = _removeDuplicates,
                Description = "Remove duplicate rows (UNION vs UNION ALL)"
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var r = Bounds;
            using var path = new SKPath();

            // Inverted triangle/funnel shape - multiple inputs converge to one output
            float topWidth = r.Width * 0.8f;
            float topOffset = (r.Width - topWidth) / 2;

            path.MoveTo(r.Left + topOffset, r.Top + HeaderHeight);
            path.LineTo(r.Right - topOffset, r.Top + HeaderHeight);
            path.LineTo(r.MidX, r.Bottom);
            path.Close();

            using var fill = new SKPaint { Color = Background, IsAntialias = true };
            using var stroke = new SKPaint { Color = Stroke, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Multiple input ports along the top edge
            if (InConnectionPoints.Count > 0)
            {
                float spacing = r.Width * 0.8f / (_inputCount + 1);
                float startX = r.Left + (r.Width - r.Width * 0.8f) / 2;

                for (int i = 0; i < InConnectionPoints.Count && i < _inputCount; i++)
                {
                    var pt = InConnectionPoints[i];
                    float x = startX + spacing * (i + 1);
                    pt.Center = new SKPoint(x, r.Top + HeaderHeight);
                    pt.Position = new SKPoint(x, r.Top + HeaderHeight - PortRadius);
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

            // Single output port at bottom
            if (OutConnectionPoints.Count > 0)
            {
                var pt = OutConnectionPoints[0];
                pt.Center = new SKPoint(r.MidX, r.Bottom);
                pt.Position = new SKPoint(r.MidX, r.Bottom + PortRadius);
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
