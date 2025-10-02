using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Cloud
{
    public enum CloudProvider { Azure, AWS, GCP, Other }
    public enum StorageType { Blob, FileShare, Queue, Table, S3, Bucket, Other }

    public class CloudStorageNode : CloudControl
    {
        private string _resourceName = "Storage";
        private CloudProvider _provider = CloudProvider.Azure;
        private StorageType _storageType = StorageType.Blob;

        public string ResourceName { get => _resourceName; set { var v = value ?? string.Empty; if (_resourceName != v) { _resourceName = v; if (NodeProperties.TryGetValue("ResourceName", out var p)) p.ParameterCurrentValue = _resourceName; else NodeProperties["ResourceName"] = new ParameterInfo { ParameterName = "ResourceName", ParameterType = typeof(string), DefaultParameterValue = _resourceName, ParameterCurrentValue = _resourceName, Description = "Resource name" }; Name = _resourceName; InvalidateVisual(); } } }
        public CloudProvider Provider { get => _provider; set { if (_provider != value) { _provider = value; if (NodeProperties.TryGetValue("Provider", out var p)) p.ParameterCurrentValue = _provider; else NodeProperties["Provider"] = new ParameterInfo { ParameterName = "Provider", ParameterType = typeof(CloudProvider), DefaultParameterValue = _provider, ParameterCurrentValue = _provider, Description = "Cloud provider", Choices = Enum.GetNames(typeof(CloudProvider)) }; InvalidateVisual(); } } }
        public StorageType StorageType { get => _storageType; set { if (_storageType != value) { _storageType = value; if (NodeProperties.TryGetValue("StorageType", out var p)) p.ParameterCurrentValue = _storageType; else NodeProperties["StorageType"] = new ParameterInfo { ParameterName = "StorageType", ParameterType = typeof(StorageType), DefaultParameterValue = _storageType, ParameterCurrentValue = _storageType, Description = "Storage type", Choices = Enum.GetNames(typeof(StorageType)) }; InvalidateVisual(); } } }

        public CloudStorageNode()
        {
            Width = 120; Height = 80;
            NodeProperties["ResourceName"] = new ParameterInfo { ParameterName = "ResourceName", ParameterType = typeof(string), DefaultParameterValue = _resourceName, ParameterCurrentValue = _resourceName, Description = "Resource name" };
            NodeProperties["Provider"] = new ParameterInfo { ParameterName = "Provider", ParameterType = typeof(CloudProvider), DefaultParameterValue = _provider, ParameterCurrentValue = _provider, Description = "Cloud provider", Choices = Enum.GetNames(typeof(CloudProvider)) };
            NodeProperties["StorageType"] = new ParameterInfo { ParameterName = "StorageType", ParameterType = typeof(StorageType), DefaultParameterValue = _storageType, ParameterCurrentValue = _storageType, Description = "Storage type", Choices = Enum.GetNames(typeof(StorageType)) };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var borderPaint = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 10, 10, fillPaint);
            canvas.DrawRoundRect(rect, 10, 10, borderPaint);

            // Simple cloud glyph at top-left
            using var glyphPaint = new SKPaint { Color = MaterialColors.Primary, Style = SKPaintStyle.Fill, IsAntialias = true };
            float gx = X + 12, gy = Y + 16;
            canvas.DrawCircle(gx, gy, 6, glyphPaint);
            canvas.DrawCircle(gx + 9, gy, 8, glyphPaint);
            canvas.DrawCircle(gx + 18, gy, 6, glyphPaint);
            canvas.DrawRect(new SKRect(gx - 6, gy, gx + 24, gy + 8), glyphPaint);

            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(ResourceName, rect.MidX, Y + Height - 16, SKTextAlign.Center, nameFont, textPaint);
            var meta = $"{Provider} · {StorageType}";
            canvas.DrawText(meta, rect.MidX, Y + Height - 4, SKTextAlign.Center, metaFont, textPaint);

            DrawPorts(canvas);
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(10f, 10f);
        }
    }
}
