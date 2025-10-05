using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Pivot node: rotates data from rows to columns (crosstab transformation).
    /// Converts row-based data into column-based aggregated view.
    /// </summary>
    public class ETLPivot : ETLControl
    {
        private string _pivotColumn = "";
        public string PivotColumn
        {
            get => _pivotColumn;
            set
            {
                var v = value ?? "";
                if (_pivotColumn == v) return;
                _pivotColumn = v;
                if (NodeProperties.TryGetValue("PivotColumn", out var p))
                    p.ParameterCurrentValue = _pivotColumn;
                InvalidateVisual();
            }
        }

        private string _groupByColumns = "";
        public string GroupByColumns
        {
            get => _groupByColumns;
            set
            {
                var v = value ?? "";
                if (_groupByColumns == v) return;
                _groupByColumns = v;
                if (NodeProperties.TryGetValue("GroupByColumns", out var p))
                    p.ParameterCurrentValue = _groupByColumns;
                InvalidateVisual();
            }
        }

        private string _valueColumn = "";
        public string ValueColumn
        {
            get => _valueColumn;
            set
            {
                var v = value ?? "";
                if (_valueColumn == v) return;
                _valueColumn = v;
                if (NodeProperties.TryGetValue("ValueColumn", out var p))
                    p.ParameterCurrentValue = _valueColumn;
                InvalidateVisual();
            }
        }

        private string _aggregateFunction = "SUM";
        public string AggregateFunction
        {
            get => _aggregateFunction;
            set
            {
                var v = value ?? "SUM";
                if (_aggregateFunction == v) return;
                _aggregateFunction = v;
                if (NodeProperties.TryGetValue("AggregateFunction", out var p))
                    p.ParameterCurrentValue = _aggregateFunction;
                InvalidateVisual();
            }
        }

        public ETLPivot()
        {
            Title = "Pivot";
            Subtitle = "Rows → Columns";
            EnsurePortCounts(1, 1);
            HeaderColor = MaterialColors.SecondaryContainer;

            NodeProperties["PivotColumn"] = new ParameterInfo
            {
                ParameterName = "PivotColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _pivotColumn,
                ParameterCurrentValue = _pivotColumn,
                Description = "Column whose values become new column headers"
            };
            NodeProperties["GroupByColumns"] = new ParameterInfo
            {
                ParameterName = "GroupByColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _groupByColumns,
                ParameterCurrentValue = _groupByColumns,
                Description = "Comma-separated columns to group by"
            };
            NodeProperties["ValueColumn"] = new ParameterInfo
            {
                ParameterName = "ValueColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _valueColumn,
                ParameterCurrentValue = _valueColumn,
                Description = "Column to aggregate for each pivot value"
            };
            NodeProperties["AggregateFunction"] = new ParameterInfo
            {
                ParameterName = "AggregateFunction",
                ParameterType = typeof(string),
                DefaultParameterValue = _aggregateFunction,
                ParameterCurrentValue = _aggregateFunction,
                Description = "Aggregation: SUM, AVG, COUNT, MIN, MAX",
                Choices = new[] { "SUM", "AVG", "COUNT", "MIN", "MAX" }
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);

            // Draw pivot rotation icon (90-degree arrow)
            var r = Bounds;
            float centerX = r.MidX;
            float centerY = r.Top + HeaderHeight + (r.Height - HeaderHeight) / 2;
            float size = 16f;

            using var iconPaint = new SKPaint
            {
                Color = MaterialColors.OnSurface.WithAlpha(128),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // Draw L-shape with arrow
            canvas.DrawLine(centerX - size, centerY + size, centerX - size, centerY - size, iconPaint);
            canvas.DrawLine(centerX - size, centerY - size, centerX + size, centerY - size, iconPaint);

            // Arrow head
            canvas.DrawLine(centerX + size, centerY - size, centerX + size - 4, centerY - size - 4, iconPaint);
            canvas.DrawLine(centerX + size, centerY - size, centerX + size - 4, centerY - size + 4, iconPaint);
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRoundRect(new SKRect(X, Y, X + Width, Y + Height), CornerRadius, CornerRadius);
            using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                canvas.DrawRoundRect(rect, fill);
            using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                canvas.DrawRoundRect(rect, border);
        }

        protected override void LayoutPorts()
        {
            float inAreaTop = Y + HeaderHeight + Padding;
            float inAreaBottom = Y + Height - Padding;
            PositionPortsAlongEdge(InConnectionPoints, new SKPoint(X, 0), inAreaTop, inAreaBottom, -1);
            PositionPortsAlongEdge(OutConnectionPoints, new SKPoint(X + Width, 0), inAreaTop, inAreaBottom, +1);
        }

        private void PositionPortsAlongEdge(System.Collections.Generic.List<IConnectionPoint> ports, SKPoint edge, float top, float bottom, int dir)
        {
            int n = Math.Max(ports.Count, 1);
            float span = Math.Max(bottom - top, 1f);
            for (int i = 0; i < ports.Count; i++)
            {
                var p = ports[i];
                float t = (i + 1) / (float)(n + 1);
                float cy = top + t * span;
                float cx = edge.X + dir * (PortRadius + 2);
                p.Center = new SKPoint(cx, cy);
                p.Position = p.Center;
                float r = PortRadius;
                p.Bounds = new SKRect(cx - r, cy - r, cx + r, cy + r);
                p.Rect = p.Bounds;
                p.Index = i;
                p.Component = this;
                p.IsAvailable = true;
            }
        }
    }
}
