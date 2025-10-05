using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Unpivot node: rotates data from columns to rows (denormalization).
    /// Converts column-based data into row-based normalized format.
    /// </summary>
    public class ETLUnpivot : ETLControl
    {
        private string _unpivotColumns = "";
        public string UnpivotColumns
        {
            get => _unpivotColumns;
            set
            {
                var v = value ?? "";
                if (_unpivotColumns == v) return;
                _unpivotColumns = v;
                if (NodeProperties.TryGetValue("UnpivotColumns", out var p))
                    p.ParameterCurrentValue = _unpivotColumns;
                InvalidateVisual();
            }
        }

        private string _attributeColumn = "Attribute";
        public string AttributeColumn
        {
            get => _attributeColumn;
            set
            {
                var v = value ?? "Attribute";
                if (_attributeColumn == v) return;
                _attributeColumn = v;
                if (NodeProperties.TryGetValue("AttributeColumn", out var p))
                    p.ParameterCurrentValue = _attributeColumn;
                InvalidateVisual();
            }
        }

        private string _valueColumn = "Value";
        public string ValueColumn
        {
            get => _valueColumn;
            set
            {
                var v = value ?? "Value";
                if (_valueColumn == v) return;
                _valueColumn = v;
                if (NodeProperties.TryGetValue("ValueColumn", out var p))
                    p.ParameterCurrentValue = _valueColumn;
                InvalidateVisual();
            }
        }

        public ETLUnpivot()
        {
            Title = "Unpivot";
            Subtitle = "Columns → Rows";
            EnsurePortCounts(1, 1);
            HeaderColor = MaterialColors.SecondaryContainer;

            NodeProperties["UnpivotColumns"] = new ParameterInfo
            {
                ParameterName = "UnpivotColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _unpivotColumns,
                ParameterCurrentValue = _unpivotColumns,
                Description = "Comma-separated columns to unpivot into rows"
            };
            NodeProperties["AttributeColumn"] = new ParameterInfo
            {
                ParameterName = "AttributeColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _attributeColumn,
                ParameterCurrentValue = _attributeColumn,
                Description = "Output column name for attribute names"
            };
            NodeProperties["ValueColumn"] = new ParameterInfo
            {
                ParameterName = "ValueColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _valueColumn,
                ParameterCurrentValue = _valueColumn,
                Description = "Output column name for attribute values"
            };
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;
            base.DrawETLContent(canvas, context);

            // Draw unpivot rotation icon (reverse 90-degree arrow)
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

            // Draw L-shape rotated (unpivot direction)
            canvas.DrawLine(centerX + size, centerY - size, centerX + size, centerY + size, iconPaint);
            canvas.DrawLine(centerX + size, centerY + size, centerX - size, centerY + size, iconPaint);

            // Arrow head
            canvas.DrawLine(centerX - size, centerY + size, centerX - size + 4, centerY + size - 4, iconPaint);
            canvas.DrawLine(centerX - size, centerY + size, centerX - size + 4, centerY + size + 4, iconPaint);
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
