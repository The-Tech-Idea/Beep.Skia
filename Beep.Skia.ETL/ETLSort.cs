using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Sort node: orders rows by specified columns and direction.
    /// </summary>
    public class ETLSort : ETLControl
    {
        private string _sortColumns = "";
        public string SortColumns
        {
            get => _sortColumns;
            set
            {
                var v = value ?? "";
                if (_sortColumns == v) return;
                _sortColumns = v;
                if (NodeProperties.TryGetValue("SortColumns", out var p))
                    p.ParameterCurrentValue = _sortColumns;
                InvalidateVisual();
            }
        }

        private string _sortDirections = "ASC";
        public string SortDirections
        {
            get => _sortDirections;
            set
            {
                var v = value ?? "ASC";
                if (_sortDirections == v) return;
                _sortDirections = v;
                if (NodeProperties.TryGetValue("SortDirections", out var p))
                    p.ParameterCurrentValue = _sortDirections;
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

        public ETLSort()
        {
            Title = "Sort";
            Subtitle = "Order Rows";
            EnsurePortCounts(1, 1);
            HeaderColor = MaterialColors.SecondaryContainer;

            NodeProperties["SortColumns"] = new ParameterInfo
            {
                ParameterName = "SortColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _sortColumns,
                ParameterCurrentValue = _sortColumns,
                Description = "Comma-separated column names to sort by"
            };
            NodeProperties["SortDirections"] = new ParameterInfo
            {
                ParameterName = "SortDirections",
                ParameterType = typeof(string),
                DefaultParameterValue = _sortDirections,
                ParameterCurrentValue = _sortDirections,
                Description = "Sort directions (ASC/DESC, comma-separated)"
            };
            NodeProperties["RemoveDuplicates"] = new ParameterInfo
            {
                ParameterName = "RemoveDuplicates",
                ParameterType = typeof(bool),
                DefaultParameterValue = _removeDuplicates,
                ParameterCurrentValue = _removeDuplicates,
                Description = "Remove duplicate rows after sorting"
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);

            // Draw sort icon (up/down arrows)
            var r = Bounds;
            float iconX = r.MidX;
            float iconY = r.Top + HeaderHeight + (r.Height - HeaderHeight) / 2;
            float arrowSize = 12f;

            using var iconPaint = new SKPaint
            {
                Color = MaterialColors.OnSurface.WithAlpha(128),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // Up arrow
            canvas.DrawLine(iconX, iconY - arrowSize, iconX, iconY, iconPaint);
            canvas.DrawLine(iconX - 4, iconY - arrowSize + 4, iconX, iconY - arrowSize, iconPaint);
            canvas.DrawLine(iconX + 4, iconY - arrowSize + 4, iconX, iconY - arrowSize, iconPaint);

            // Down arrow
            canvas.DrawLine(iconX + 8, iconY, iconX + 8, iconY + arrowSize, iconPaint);
            canvas.DrawLine(iconX + 4, iconY + arrowSize - 4, iconX + 8, iconY + arrowSize, iconPaint);
            canvas.DrawLine(iconX + 12, iconY + arrowSize - 4, iconX + 8, iconY + arrowSize, iconPaint);
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
