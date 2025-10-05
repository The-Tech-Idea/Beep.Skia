using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// ETL Lookup component that enriches data by joining with reference data.
    /// Displays as a cylinder with two inputs (main data + reference data).
    /// Supports caching strategies for performance optimization.
    /// </summary>
    public class ETLLookup : ETLControl
    {
        private CacheMode _cacheMode = CacheMode.Full;
        public CacheMode CacheMode
        {
            get => _cacheMode;
            set
            {
                if (_cacheMode == value) return;
                _cacheMode = value;
                if (NodeProperties.TryGetValue("CacheMode", out var p))
                    p.ParameterCurrentValue = _cacheMode;
                else
                    NodeProperties["CacheMode"] = new ParameterInfo
                    {
                        ParameterName = "CacheMode",
                        ParameterType = typeof(CacheMode),
                        DefaultParameterValue = _cacheMode,
                        ParameterCurrentValue = _cacheMode,
                        Description = "Cache strategy: None, Partial, Full",
                        Choices = System.Enum.GetNames(typeof(CacheMode))
                    };
                InvalidateVisual();
            }
        }

        private string _matchColumns = string.Empty;
        public string MatchColumns
        {
            get => _matchColumns;
            set
            {
                var v = value ?? string.Empty;
                if (_matchColumns == v) return;
                _matchColumns = v;
                if (NodeProperties.TryGetValue("MatchColumns", out var p))
                    p.ParameterCurrentValue = _matchColumns;
                else
                    NodeProperties["MatchColumns"] = new ParameterInfo
                    {
                        ParameterName = "MatchColumns",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _matchColumns,
                        ParameterCurrentValue = _matchColumns,
                        Description = "Columns to match (CSV: MainCol=RefCol,...)"
                    };
                InvalidateVisual();
            }
        }

        private string _returnColumns = string.Empty;
        public string ReturnColumns
        {
            get => _returnColumns;
            set
            {
                var v = value ?? string.Empty;
                if (_returnColumns == v) return;
                _returnColumns = v;
                if (NodeProperties.TryGetValue("ReturnColumns", out var p))
                    p.ParameterCurrentValue = _returnColumns;
                else
                    NodeProperties["ReturnColumns"] = new ParameterInfo
                    {
                        ParameterName = "ReturnColumns",
                        ParameterType = typeof(string),
                        DefaultParameterValue = _returnColumns,
                        ParameterCurrentValue = _returnColumns,
                        Description = "Columns to return from reference (CSV)"
                    };
                InvalidateVisual();
            }
        }

        private bool _failOnNoMatch = false;
        public bool FailOnNoMatch
        {
            get => _failOnNoMatch;
            set
            {
                if (_failOnNoMatch == value) return;
                _failOnNoMatch = value;
                if (NodeProperties.TryGetValue("FailOnNoMatch", out var p))
                    p.ParameterCurrentValue = _failOnNoMatch;
                else
                    NodeProperties["FailOnNoMatch"] = new ParameterInfo
                    {
                        ParameterName = "FailOnNoMatch",
                        ParameterType = typeof(bool),
                        DefaultParameterValue = _failOnNoMatch,
                        ParameterCurrentValue = _failOnNoMatch,
                        Description = "Fail if lookup finds no match"
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

        public ETLLookup()
        {
            Title = "Lookup";
            HeaderColor = MaterialControl.MaterialColors.TertiaryContainer;
            Width = 160;
            Height = 90;
            DisplayText = "Lookup";
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            // 2 inputs: main data (port 0) + reference data (port 1)
            EnsurePortCounts(inCount: 2, outCount: 1);

            NodeProperties["CacheMode"] = new ParameterInfo
            {
                ParameterName = "CacheMode",
                ParameterType = typeof(CacheMode),
                DefaultParameterValue = _cacheMode,
                ParameterCurrentValue = _cacheMode,
                Description = "Cache strategy: None, Partial, Full",
                Choices = System.Enum.GetNames(typeof(CacheMode))
            };
            NodeProperties["MatchColumns"] = new ParameterInfo
            {
                ParameterName = "MatchColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _matchColumns,
                ParameterCurrentValue = _matchColumns,
                Description = "Columns to match (CSV: MainCol=RefCol,...)"
            };
            NodeProperties["ReturnColumns"] = new ParameterInfo
            {
                ParameterName = "ReturnColumns",
                ParameterType = typeof(string),
                DefaultParameterValue = _returnColumns,
                ParameterCurrentValue = _returnColumns,
                Description = "Columns to return from reference (CSV)"
            };
            NodeProperties["FailOnNoMatch"] = new ParameterInfo
            {
                ParameterName = "FailOnNoMatch",
                ParameterType = typeof(bool),
                DefaultParameterValue = _failOnNoMatch,
                ParameterCurrentValue = _failOnNoMatch,
                Description = "Fail if lookup finds no match"
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

            // Draw double cylinder to indicate lookup/reference data merge
            using var fill = new SKPaint
            {
                Color = Background,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            // Main cylinder
            canvas.DrawRect(new SKRect(rect.Left, rect.Top + 8, rect.Right - 20, rect.Bottom - 8), fill);
            canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right - 20, rect.Top + 16), fill);
            canvas.DrawOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right - 20, rect.Bottom), fill);

            // Reference cylinder (offset, smaller)
            var refAlpha = (byte)(Background.Alpha * 0.6f);
            var refColor = Background.WithAlpha(refAlpha);
            using var refFill = new SKPaint { Color = refColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRect(new SKRect(rect.Left + 20, rect.Top + 12, rect.Right, rect.Bottom - 4), refFill);
            canvas.DrawOval(new SKRect(rect.Left + 20, rect.Top + 4, rect.Right, rect.Top + 20), refFill);
            canvas.DrawOval(new SKRect(rect.Left + 20, rect.Bottom - 12, rect.Right, rect.Bottom), refFill);

            // Border
            using var border = new SKPaint
            {
                Color = Stroke,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.25f,
                IsAntialias = true
            };
            using var path = new SKPath();
            path.AddOval(new SKRect(rect.Left, rect.Top, rect.Right - 20, rect.Top + 16));
            path.AddRect(new SKRect(rect.Left, rect.Top + 8, rect.Right - 20, rect.Bottom - 8));
            path.AddOval(new SKRect(rect.Left, rect.Bottom - 16, rect.Right - 20, rect.Bottom));
            canvas.DrawPath(path, border);
        }

        protected override void LayoutPorts()
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);

            // Input 0 (main data): left side, upper
            if (InConnectionPoints.Count > 0)
            {
                var mainIn = InConnectionPoints[0];
                mainIn.Center = new SKPoint(rect.Left - PortRadius - 2, rect.Top + Height * 0.33f);
                mainIn.Position = mainIn.Center;
                float r = PortRadius;
                mainIn.Bounds = new SKRect(mainIn.Center.X - r, mainIn.Center.Y - r, mainIn.Center.X + r, mainIn.Center.Y + r);
                mainIn.Rect = mainIn.Bounds;
                mainIn.Index = 0;
                mainIn.Component = this;
                mainIn.IsAvailable = true;
            }

            // Input 1 (reference data): left side, lower
            if (InConnectionPoints.Count > 1)
            {
                var refIn = InConnectionPoints[1];
                refIn.Center = new SKPoint(rect.Left - PortRadius - 2, rect.Top + Height * 0.67f);
                refIn.Position = refIn.Center;
                float r = PortRadius;
                refIn.Bounds = new SKRect(refIn.Center.X - r, refIn.Center.Y - r, refIn.Center.X + r, refIn.Center.Y + r);
                refIn.Rect = refIn.Bounds;
                refIn.Index = 1;
                refIn.Component = this;
                refIn.IsAvailable = true;
            }

            // Output: right side, middle
            if (OutConnectionPoints.Count > 0)
            {
                var output = OutConnectionPoints[0];
                output.Center = new SKPoint(rect.Right + PortRadius + 2, rect.MidY);
                output.Position = output.Center;
                float r = PortRadius;
                output.Bounds = new SKRect(output.Center.X - r, output.Center.Y - r, output.Center.X + r, output.Center.Y + r);
                output.Rect = output.Bounds;
                output.Index = 0;
                output.Component = this;
                output.IsAvailable = true;
            }
        }
    }
}
