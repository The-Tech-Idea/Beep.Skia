using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Cloud
{
    public class CloudComputeNode : CloudControl
    {
        private string _vmName = "VM";
        private CloudProvider _provider = CloudProvider.Azure;
        private int _cpuCores = 2;
        private int _memoryGB = 8;

        public string VmName { get => _vmName; set { var v = value ?? string.Empty; if (_vmName != v) { _vmName = v; if (NodeProperties.TryGetValue("VmName", out var p)) p.ParameterCurrentValue = _vmName; else NodeProperties["VmName"] = new ParameterInfo { ParameterName = "VmName", ParameterType = typeof(string), DefaultParameterValue = _vmName, ParameterCurrentValue = _vmName, Description = "VM name" }; Name = _vmName; InvalidateVisual(); } } }
        public CloudProvider Provider { get => _provider; set { if (_provider != value) { _provider = value; if (NodeProperties.TryGetValue("Provider", out var p)) p.ParameterCurrentValue = _provider; else NodeProperties["Provider"] = new ParameterInfo { ParameterName = "Provider", ParameterType = typeof(CloudProvider), DefaultParameterValue = _provider, ParameterCurrentValue = _provider, Description = "Cloud provider", Choices = Enum.GetNames(typeof(CloudProvider)) }; InvalidateVisual(); } } }
        public int CpuCores { get => _cpuCores; set { int v = Math.Max(1, value); if (_cpuCores != v) { _cpuCores = v; if (NodeProperties.TryGetValue("CpuCores", out var p)) p.ParameterCurrentValue = _cpuCores; else NodeProperties["CpuCores"] = new ParameterInfo { ParameterName = "CpuCores", ParameterType = typeof(int), DefaultParameterValue = _cpuCores, ParameterCurrentValue = _cpuCores, Description = "CPU cores" }; InvalidateVisual(); } } }
        public int MemoryGB { get => _memoryGB; set { int v = Math.Max(1, value); if (_memoryGB != v) { _memoryGB = v; if (NodeProperties.TryGetValue("MemoryGB", out var p)) p.ParameterCurrentValue = _memoryGB; else NodeProperties["MemoryGB"] = new ParameterInfo { ParameterName = "MemoryGB", ParameterType = typeof(int), DefaultParameterValue = _memoryGB, ParameterCurrentValue = _memoryGB, Description = "Memory GB" }; InvalidateVisual(); } } }

        public CloudComputeNode()
        {
            Width = 120; Height = 90;
            NodeProperties["VmName"] = new ParameterInfo { ParameterName = "VmName", ParameterType = typeof(string), DefaultParameterValue = _vmName, ParameterCurrentValue = _vmName, Description = "VM name" };
            NodeProperties["Provider"] = new ParameterInfo { ParameterName = "Provider", ParameterType = typeof(CloudProvider), DefaultParameterValue = _provider, ParameterCurrentValue = _provider, Description = "Cloud provider", Choices = Enum.GetNames(typeof(CloudProvider)) };
            NodeProperties["CpuCores"] = new ParameterInfo { ParameterName = "CpuCores", ParameterType = typeof(int), DefaultParameterValue = _cpuCores, ParameterCurrentValue = _cpuCores, Description = "CPU cores" };
            NodeProperties["MemoryGB"] = new ParameterInfo { ParameterName = "MemoryGB", ParameterType = typeof(int), DefaultParameterValue = _memoryGB, ParameterCurrentValue = _memoryGB, Description = "Memory GB" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var borderPaint = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);

            // Small compute chip icon
            using var chip = new SKPaint { Color = MaterialColors.Primary, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            float cx = rect.Left + 14, cy = rect.Top + 16;
            canvas.DrawRect(new SKRect(cx - 8, cy - 6, cx + 8, cy + 6), chip);
            for (int i = -1; i <= 1; i++) { canvas.DrawLine(cx + i * 8, cy - 10, cx + i * 8, cy - 6, chip); canvas.DrawLine(cx + i * 8, cy + 6, cx + i * 8, cy + 10, chip); }

            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(VmName, rect.MidX, rect.MidY + 6, SKTextAlign.Center, nameFont, textPaint);
            var meta = $"{Provider} · {CpuCores} vCPU · {MemoryGB} GB";
            canvas.DrawText(meta, rect.MidX, rect.Bottom - 6, SKTextAlign.Center, metaFont, textPaint);

            DrawPorts(canvas);
        }
    }
}
