using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL SCD (Slowly Changing Dimension) component for managing dimension updates.
    /// Displays as a special cylinder with historical tracking icon.
    /// Supports Type 1 (overwrite) and Type 2 (historical with effective dates).
    /// </summary>
    public class ETLScd : ETLControl
    {
        private ScdType _scdType = ScdType.Type1;
        public ScdType Type
        {
            get => _scdType;
            set
            {
                if (_scdType == value) return;
                _scdType = value;
                if (NodeProperties.TryGetValue("ScdType", out var p))
                    p.ParameterCurrentValue = _scdType;
                else
                    NodeProperties["ScdType"] = new ParameterInfo
                    {
                        ParameterName = "ScdType",
                        ParameterType = typeof(ScdType),
                        DefaultParameterValue = _scdType,
                        ParameterCurrentValue = _scdType,
                        Description = "SCD Type: Type1 (overwrite), Type2 (historical)",
                        Choices = System.Enum.GetNames(typeof(ScdType))
                    };
                InvalidateVisual();
            }
        }

        private string _businessKeys = string.Empty;
        public string BusinessKeys
        {
            get => _businessKeys;
            set
            {
                var v = value ?? string.Empty;
                if (_businessKeys == v) return;
                _businessKeys = v;
                if (NodeProperties.TryGetValue("BusinessKeys", out var p))
                    p.ParameterCurrentValue = _businessKeys;
                else
                    NodeProperties["BusinessKeys"] = new ParameterInfo
                    {
                        ParameterName = "BusinessKeys",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _businessKeys,
                        ParameterCurrentValue = _businessKeys,
                        Description = "Business key columns (CSV) for matching existing records"
                    };
                InvalidateVisual();
            }
        }

        private string _changeTrackingColumns = string.Empty;
        public string ChangeTrackingColumns
        {
            get => _changeTrackingColumns;
            set
            {
                var v = value ?? string.Empty;
                if (_changeTrackingColumns == v) return;
                _changeTrackingColumns = v;
                if (NodeProperties.TryGetValue("ChangeTrackingColumns", out var p))
                    p.ParameterCurrentValue = _changeTrackingColumns;
                else
                    NodeProperties["ChangeTrackingColumns"] = new ParameterInfo
                    {
                        ParameterName = "ChangeTrackingColumns",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _changeTrackingColumns,
                        ParameterCurrentValue = _changeTrackingColumns,
                        Description = "Columns to track for changes (CSV)"
                    };
                InvalidateVisual();
            }
        }

        private string _effectiveFromColumn = "EffectiveFrom";
        public string EffectiveFromColumn
        {
            get => _effectiveFromColumn;
            set
            {
                var v = value ?? "EffectiveFrom";
                if (_effectiveFromColumn == v) return;
                _effectiveFromColumn = v;
                if (NodeProperties.TryGetValue("EffectiveFromColumn", out var p))
                    p.ParameterCurrentValue = _effectiveFromColumn;
                else
                    NodeProperties["EffectiveFromColumn"] = new ParameterInfo
                    {
                        ParameterName = "EffectiveFromColumn",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _effectiveFromColumn,
                        ParameterCurrentValue = _effectiveFromColumn,
                        Description = "Column name for effective from date (Type 2 only)"
                    };
                InvalidateVisual();
            }
        }

        private string _effectiveToColumn = "EffectiveTo";
        public string EffectiveToColumn
        {
            get => _effectiveToColumn;
            set
            {
                var v = value ?? "EffectiveTo";
                if (_effectiveToColumn == v) return;
                _effectiveToColumn = v;
                if (NodeProperties.TryGetValue("EffectiveToColumn", out var p))
                    p.ParameterCurrentValue = _effectiveToColumn;
                else
                    NodeProperties["EffectiveToColumn"] = new ParameterInfo
                    {
                        ParameterName = "EffectiveToColumn",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _effectiveToColumn,
                        ParameterCurrentValue = _effectiveToColumn,
                        Description = "Column name for effective to date (Type 2 only)"
                    };
                InvalidateVisual();
            }
        }

        private string _currentFlagColumn = "IsCurrent";
        public string CurrentFlagColumn
        {
            get => _currentFlagColumn;
            set
            {
                var v = value ?? "IsCurrent";
                if (_currentFlagColumn == v) return;
                _currentFlagColumn = v;
                if (NodeProperties.TryGetValue("CurrentFlagColumn", out var p))
                    p.ParameterCurrentValue = _currentFlagColumn;
                else
                    NodeProperties["CurrentFlagColumn"] = new ParameterInfo
                    {
                        ParameterName = "CurrentFlagColumn",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _currentFlagColumn,
                        ParameterCurrentValue = _currentFlagColumn,
                        Description = "Column name for current flag (Type 2 only)"
                    };
                InvalidateVisual();
            }
        }

        private string _tableName = string.Empty;
        public string TableName
        {
            get => _tableName;
            set
            {
                var v = value ?? string.Empty;
                if (_tableName == v) return;
                _tableName = v;
                if (NodeProperties.TryGetValue("TableName", out var p))
                    p.ParameterCurrentValue = _tableName;
                else
                    NodeProperties["TableName"] = new ParameterInfo
                    {
                        ParameterName = "TableName",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _tableName,
                        ParameterCurrentValue = _tableName,
                        Description = "Target dimension table name"
                    };
                InvalidateVisual();
            }
        }

        public ETLScd()
        {
            Title = "SCD";
            HeaderColor = MaterialControl.MaterialColors.TertiaryContainer;
            Width = 160;
            Height = 90;
            DisplayText = "SCD";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            // 1 input (incoming dimension records), 1 output (merged records)
            EnsurePortCounts(inCount: 1, outCount: 1);

            NodeProperties["ScdType"] = new ParameterInfo
            {
                ParameterName = "ScdType",
                ParameterType = typeof(ScdType),
                DefaultParameterValue = _scdType,
                ParameterCurrentValue = _scdType,
                Description = "SCD Type: Type1 (overwrite), Type2 (historical)",
                Choices = System.Enum.GetNames(typeof(ScdType))
            };
            NodeProperties["BusinessKeys"] = new ParameterInfo
            {
                ParameterName = "BusinessKeys",
                ParameterType = typeof(string),
                DefaultParameterValue = _businessKeys,
                ParameterCurrentValue = _businessKeys,
                Description = "Business key columns (CSV) for matching existing records"
            };
            NodeProperties["ChangeTrackingColumns"] = new ParameterInfo
            {
                ParameterName = "ChangeTrackingColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _changeTrackingColumns,
                ParameterCurrentValue = _changeTrackingColumns,
                Description = "Columns to track for changes (CSV)"
            };
            NodeProperties["EffectiveFromColumn"] = new ParameterInfo
            {
                ParameterName = "EffectiveFromColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _effectiveFromColumn,
                ParameterCurrentValue = _effectiveFromColumn,
                Description = "Column name for effective from date (Type 2 only)"
            };
            NodeProperties["EffectiveToColumn"] = new ParameterInfo
            {
                ParameterName = "EffectiveToColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _effectiveToColumn,
                ParameterCurrentValue = _effectiveToColumn,
                Description = "Column name for effective to date (Type 2 only)"
            };
            NodeProperties["CurrentFlagColumn"] = new ParameterInfo
            {
                ParameterName = "CurrentFlagColumn",
                ParameterType = typeof(string),
                DefaultParameterValue = _currentFlagColumn,
                ParameterCurrentValue = _currentFlagColumn,
                Description = "Column name for current flag (Type 2 only)"
            };
            NodeProperties["TableName"] = new ParameterInfo
            {
                ParameterName = "TableName",
                ParameterType = typeof(string),
                DefaultParameterValue = _tableName,
                ParameterCurrentValue = _tableName,
                Description = "Target dimension table name"
            };
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw cylinder with clock icon overlay to indicate historical tracking
            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            // Cylinder body
            canvas.DrawRect(new SKRect(rect.Left, rect.Top + 8, rect.Right, rect.Bottom - 8), fill);
            canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16), fill);
            canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom), fill);

            // Clock/history icon (small circle with hand)
            var clockCenter = new SKPoint(rect.Right - 20, rect.Top + 20);
            var clockRadius = 10f;
            using var clockFill = new SKPaint
            {
                Color = MaterialControl.MaterialColors.Tertiary.WithAlpha(200),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(clockCenter, clockRadius, clockFill);
            using var clockBorder = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };
            canvas.DrawCircle(clockCenter, clockRadius, clockBorder);
            // Clock hands
            canvas.DrawLine(clockCenter, clockCenter + new SKPoint(0, -6), clockBorder);
            canvas.DrawLine(clockCenter, clockCenter + new SKPoint(4, 0), clockBorder);

            // Border
            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            using var path = new SKPath();
            path.AddOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + 16));
            path.AddRect(new SKRect(rect.Left, rect.Top + 8, rect.Right, rect.Bottom - 8));
            path.AddOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right, rect.Bottom));
            canvas.DrawPath(path, border);
        }

        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Input at left middle
            if (InConnectionPoints.Count > 0)
            {
                var inputPoint = InConnectionPoints[0];
                inputPoint.Center = new SKPoint(rect.Left - PortRadius - 2, rect.MidY);
                inputPoint.Position = inputPoint.Center;
                float r = PortRadius;
                inputPoint.Bounds = new SKRect(inputPoint.Center.X - r, inputPoint.Center.Y - r,
                                             inputPoint.Center.X + r, inputPoint.Center.Y + r);
                inputPoint.Rect = inputPoint.Bounds;
                inputPoint.Index = 0;
                inputPoint.Component = this;
                inputPoint.IsAvailable = true;
            }

            // Output at right middle
            if (OutConnectionPoints.Count > 0)
            {
                var outputPoint = OutConnectionPoints[0];
                outputPoint.Center = new SKPoint(rect.Right + PortRadius + 2, rect.MidY);
                outputPoint.Position = outputPoint.Center;
                float r = PortRadius;
                outputPoint.Bounds = new SKRect(outputPoint.Center.X - r, outputPoint.Center.Y - r,
                                              outputPoint.Center.X + r, outputPoint.Center.Y + r);
                outputPoint.Rect = outputPoint.Bounds;
                outputPoint.Index = 0;
                outputPoint.Component = this;
                outputPoint.IsAvailable = true;
            }
        }
    }
}
