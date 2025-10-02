using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Security
{
    public enum AssetCategory { Application, Data, Infrastructure, Identity, Other }
    public enum Criticality { Low, Medium, High, Critical }

    public class AssetNode : SecurityControl
    {
        private string _assetName = "Asset";
        private AssetCategory _category = AssetCategory.Application;
        private Criticality _criticality = Criticality.Medium;

        public string AssetName { get => _assetName; set { var v = value ?? string.Empty; if (_assetName != v) { _assetName = v; if (NodeProperties.TryGetValue("AssetName", out var p)) p.ParameterCurrentValue = _assetName; else NodeProperties["AssetName"] = new ParameterInfo { ParameterName = "AssetName", ParameterType = typeof(string), DefaultParameterValue = _assetName, ParameterCurrentValue = _assetName, Description = "Asset name" }; Name = _assetName; InvalidateVisual(); } } }
        public AssetCategory Category { get => _category; set { if (_category != value) { _category = value; if (NodeProperties.TryGetValue("Category", out var p)) p.ParameterCurrentValue = _category; else NodeProperties["Category"] = new ParameterInfo { ParameterName = "Category", ParameterType = typeof(AssetCategory), DefaultParameterValue = _category, ParameterCurrentValue = _category, Description = "Asset category", Choices = Enum.GetNames(typeof(AssetCategory)) }; InvalidateVisual(); } } }
        public Criticality Criticality { get => _criticality; set { if (_criticality != value) { _criticality = value; if (NodeProperties.TryGetValue("Criticality", out var p)) p.ParameterCurrentValue = _criticality; else NodeProperties["Criticality"] = new ParameterInfo { ParameterName = "Criticality", ParameterType = typeof(Criticality), DefaultParameterValue = _criticality, ParameterCurrentValue = _criticality, Description = "Criticality", Choices = Enum.GetNames(typeof(Criticality)) }; InvalidateVisual(); } } }

        public AssetNode()
        {
            Width = 140; Height = 80;
            NodeProperties["AssetName"] = new ParameterInfo { ParameterName = "AssetName", ParameterType = typeof(string), DefaultParameterValue = _assetName, ParameterCurrentValue = _assetName, Description = "Asset name" };
            NodeProperties["Category"] = new ParameterInfo { ParameterName = "Category", ParameterType = typeof(AssetCategory), DefaultParameterValue = _category, ParameterCurrentValue = _category, Description = "Asset category", Choices = Enum.GetNames(typeof(AssetCategory)) };
            NodeProperties["Criticality"] = new ParameterInfo { ParameterName = "Criticality", ParameterType = typeof(Criticality), DefaultParameterValue = _criticality, ParameterCurrentValue = _criticality, Description = "Criticality", Choices = Enum.GetNames(typeof(Criticality)) };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawSecurityContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var r = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(r, 6, 6, fill);
            canvas.DrawRoundRect(r, 6, 6, border);

            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(AssetName, r.MidX, r.MidY, SKTextAlign.Center, nameFont, text);
            canvas.DrawText($"{Category} · {Criticality}", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, text);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
