using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System.Collections.Generic;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Derived Column component for adding calculated columns to the data flow.
    /// Displays as a rectangle with expression builder support.
    /// Supports multiple derived columns with expressions using built-in function library.
    /// </summary>
    public class ETLDerivedColumn : ETLControl
    {
        private string _derivedColumnsJson = "[]";
        public string DerivedColumns
        {
            get => _derivedColumnsJson;
            set
            {
                var v = value ?? "[]";
                if (_derivedColumnsJson == v) return;
                _derivedColumnsJson = v;
                if (NodeProperties.TryGetValue("DerivedColumns", out var p))
                    p.ParameterCurrentValue = _derivedColumnsJson;
                else
                    NodeProperties["DerivedColumns"] = new ParameterInfo
                    {
                        ParameterName = "DerivedColumns",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _derivedColumnsJson,
                        ParameterCurrentValue = _derivedColumnsJson,
                        Description = "Derived columns (JSON array of DerivedColumnDefinition)"
                    };
                InvalidateVisual();
            }
        }

        private string _outputSchemaJson = "[]";
        public string OutputSchema
        {
            get => _outputSchemaJson;
            set
            {
                var v = value ?? "[]";
                if (_outputSchemaJson == v) return;
                _outputSchemaJson = v;
                if (NodeProperties.TryGetValue("OutputSchema", out var p))
                    p.ParameterCurrentValue = _outputSchemaJson;
                else
                    NodeProperties["OutputSchema"] = new ParameterInfo
                    {
                        ParameterName = "OutputSchema",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _outputSchemaJson,
                        ParameterCurrentValue = _outputSchemaJson,
                        Description = "Output schema (JSON array of ColumnDefinition)"
                    };
                InvalidateVisual();
            }
        }

        public ETLDerivedColumn()
        {
            Title = "Derived Column";
            HeaderColor = MaterialControl.MaterialColors.PrimaryContainer;
            Width = 160;
            Height = 80;
            DisplayText = "Derived";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            EnsurePortCounts(inCount: 1, outCount: 1);

            NodeProperties["DerivedColumns"] = new ParameterInfo
            {
                ParameterName = "DerivedColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _derivedColumnsJson,
                ParameterCurrentValue = _derivedColumnsJson,
                Description = "Derived columns (JSON array of DerivedColumnDefinition)"
            };
            NodeProperties["OutputSchema"] = new ParameterInfo
            {
                ParameterName = "OutputSchema",
                ParameterType = typeof(string),
                DefaultParameterValue = _outputSchemaJson,
                ParameterCurrentValue = _outputSchemaJson,
                Description = "Output schema (JSON array of ColumnDefinition)"
            };
        }

        protected override void DrawShape(SKCanvas canvas)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Draw rounded rectangle
            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, 8, 8, fill);

            // Draw "f(x)" symbol to indicate function/expression
            using var textPaint = new SKPaint
            {
                Color = Stroke.WithAlpha((byte)(Stroke.Alpha * 0.3f)),
                TextSize = 24,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic)
            };
            var fxText = "f(x)";
            var textBounds = new SKRect();
            textPaint.MeasureText(fxText, ref textBounds);
            canvas.DrawText(fxText, rect.MidX - textBounds.MidX, rect.MidY - textBounds.MidY, textPaint);

            // Border
            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, 8, 8, border);
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
