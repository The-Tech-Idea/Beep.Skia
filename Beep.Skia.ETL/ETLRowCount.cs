using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Row Count node: counts rows passing through and optionally adds count column.
    /// Useful for auditing and debugging pipeline data flow.
    /// </summary>
    public class ETLRowCount : ETLControl
    {
        private bool _addCountColumn = false;
        public bool AddCountColumn
        {
            get => _addCountColumn;
            set
            {
                if (_addCountColumn == value) return;
                _addCountColumn = value;
                if (NodeProperties.TryGetValue("AddCountColumn", out var p))
                    p.ParameterCurrentValue = _addCountColumn;
                InvalidateVisual();
            }
        }

        private string _countColumnName = "RowNumber";
        public string CountColumnName
        {
            get => _countColumnName;
            set
            {
                var v = value ?? "RowNumber";
                if (_countColumnName == v) return;
                _countColumnName = v;
                if (NodeProperties.TryGetValue("CountColumnName", out var p))
                    p.ParameterCurrentValue = _countColumnName;
                InvalidateVisual();
            }
        }

        public ETLRowCount()
        {
            Title = "Row Count";
            Subtitle = "Audit/Number Rows";
            EnsurePortCounts(1, 1);
            HeaderColor = MaterialColors.TertiaryContainer;

            NodeProperties["AddCountColumn"] = new ParameterInfo
            {
                ParameterName = "AddCountColumn",
                ParameterType = typeof(bool),
                DefaultParameterValue = _addCountColumn,
                ParameterCurrentValue = _addCountColumn,
                Description = "Add row number column to output"
            };
            NodeProperties["CountColumnName"] = new ParameterInfo
            {
                ParameterName = "CountColumnName",
                ParameterType = typeof(string),
                DefaultParameterValue = _countColumnName,
                ParameterCurrentValue = _countColumnName,
                Description = "Name of the count column if added"
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);

            // Draw counter icon (123...)
            var r = Bounds;
            float centerX = r.MidX;
            float centerY = r.Top + HeaderHeight + (r.Height - HeaderHeight) / 2;

            using var textPaint = new SKPaint
            {
                Color = MaterialColors.OnSurface.WithAlpha(128),
                IsAntialias = true
            };
            using var font = new SKFont { Size = 14 };

            string icon = "1 2 3";
            float iconWidth = font.MeasureText(icon, textPaint);
            canvas.DrawText(icon, centerX - iconWidth / 2, centerY + 5, SKTextAlign.Left, font, textPaint);
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
