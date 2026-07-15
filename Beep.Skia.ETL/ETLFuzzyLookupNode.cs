using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.ETL
{
    public class ETLFuzzyLookupNode : ETLControl
    {
        public enum FuzzyAlgorithm { Levenshtein, Soundex, Metaphone, JaroWinkler }

        private FuzzyAlgorithm _algorithm = FuzzyAlgorithm.Levenshtein;
        private double _threshold = 0.8;
        private string _matchColumn = "";
        private string _referenceColumn = "";

        public FuzzyAlgorithm Algorithm { get => _algorithm; set { if (_algorithm == value) return; _algorithm = value; SetProp("Algorithm", value); InvalidateVisual(); } }
        public double Threshold { get => _threshold; set { var v = Math.Max(0, Math.Min(1, value)); if (Math.Abs(_threshold - v) < 0.001) return; _threshold = v; SetProp("Threshold", v); InvalidateVisual(); } }
        public string MatchColumn { get => _matchColumn; set { var v = value ?? ""; if (_matchColumn == v) return; _matchColumn = v; SetProp("MatchColumn", v); InvalidateVisual(); } }
        public string ReferenceColumn { get => _referenceColumn; set { var v = value ?? ""; if (_referenceColumn == v) return; _referenceColumn = v; SetProp("ReferenceColumn", v); InvalidateVisual(); } }

        private void SetProp(string name, object val, string desc = null, string[] choices = null)
        {
            if (NodeProperties.TryGetValue(name, out var p) && p != null) p.ParameterCurrentValue = val;
            else NodeProperties[name] = new ParameterInfo { ParameterName = name, ParameterType = val.GetType(), DefaultParameterValue = val, ParameterCurrentValue = val, Description = desc ?? name, Choices = choices };
        }

        public ETLFuzzyLookupNode()
        {
            Width = 140; Height = 80; Name = "FuzzyLookup";
            EnsurePortCounts(2, 1);
            SetProp("Algorithm", _algorithm, "Fuzzy matching algorithm", Enum.GetNames(typeof(FuzzyAlgorithm)));
            SetProp("Threshold", _threshold, "Similarity threshold (0-1)");
            SetProp("MatchColumn", _matchColumn, "Column to match");
            SetProp("ReferenceColumn", _referenceColumn, "Reference lookup column");
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x4C, 0xAF, 0x50), StrokeWidth = 2f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 8, 8, fill);
            canvas.DrawRoundRect(r, 8, 8, stroke);
            using var icon = new SKPaint { Color = new SKColor(0x4C, 0xAF, 0x50), StrokeWidth = 2f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawCircle(r.MidX - 15, r.MidY - 4, 10, icon);
            canvas.DrawLine(r.MidX - 7, r.MidY - 1, r.MidX + 8, r.MidY + 8, icon);
            using var font = new SKFont(SKTypeface.Default, 9);
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText("Fuzzy", r.MidX + 16, r.MidY, font, text);
            canvas.DrawText("Lookup", r.MidX + 16, r.MidY + 12, font, text);
            DrawPorts(canvas);
        }

        protected override void LayoutPorts() { LayoutPortsVerticalSegments(Height * 0.2f, Height * 0.2f); }
    }
}
