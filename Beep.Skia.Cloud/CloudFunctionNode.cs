using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Cloud
{
    public enum RuntimeKind { DotNet, NodeJs, Python, Java, Go }

    public class CloudFunctionNode : CloudControl
    {
        private string _name = "Function";
        private RuntimeKind _runtime = RuntimeKind.DotNet;
        private int _memoryMB = 256;

        public string FunctionName { get => _name; set { var v = value ?? string.Empty; if (_name != v) { _name = v; if (NodeProperties.TryGetValue("FunctionName", out var p)) p.ParameterCurrentValue = _name; else NodeProperties["FunctionName"] = new ParameterInfo { ParameterName = "FunctionName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Function name" }; Name = _name; InvalidateVisual(); } } }
        public RuntimeKind Runtime { get => _runtime; set { if (_runtime != value) { _runtime = value; if (NodeProperties.TryGetValue("Runtime", out var p)) p.ParameterCurrentValue = _runtime; else NodeProperties["Runtime"] = new ParameterInfo { ParameterName = "Runtime", ParameterType = typeof(RuntimeKind), DefaultParameterValue = _runtime, ParameterCurrentValue = _runtime, Description = "Runtime", Choices = Enum.GetNames(typeof(RuntimeKind)) }; InvalidateVisual(); } } }
        public int MemoryMB { get => _memoryMB; set { var v = Math.Max(64, Math.Min(8192, value)); if (_memoryMB != v) { _memoryMB = v; if (NodeProperties.TryGetValue("MemoryMB", out var p)) p.ParameterCurrentValue = _memoryMB; else NodeProperties["MemoryMB"] = new ParameterInfo { ParameterName = "MemoryMB", ParameterType = typeof(int), DefaultParameterValue = _memoryMB, ParameterCurrentValue = _memoryMB, Description = "Memory (MB)" }; InvalidateVisual(); } } }

        public CloudFunctionNode()
        {
            Width = 160; Height = 80;
            NodeProperties["FunctionName"] = new ParameterInfo { ParameterName = "FunctionName", ParameterType = typeof(string), DefaultParameterValue = _name, ParameterCurrentValue = _name, Description = "Function name" };
            NodeProperties["Runtime"] = new ParameterInfo { ParameterName = "Runtime", ParameterType = typeof(RuntimeKind), DefaultParameterValue = _runtime, ParameterCurrentValue = _runtime, Description = "Runtime", Choices = Enum.GetNames(typeof(RuntimeKind)) };
            NodeProperties["MemoryMB"] = new ParameterInfo { ParameterName = "MemoryMB", ParameterType = typeof(int), DefaultParameterValue = _memoryMB, ParameterCurrentValue = _memoryMB, Description = "Memory (MB)" };
            EnsurePortCounts(1, 1);
        }

        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, IsAntialias = true };
            using var border = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, IsAntialias = true, Style = SKPaintStyle.Stroke };
            canvas.DrawRoundRect(r, 8, 8, fill);
            canvas.DrawRoundRect(r, 8, 8, border);

            using var namePaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var metaPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            using var metaFont = new SKFont(SKTypeface.Default, 8);
            canvas.DrawText(FunctionName, r.MidX, r.MidY - 6, SKTextAlign.Center, nameFont, namePaint);
            canvas.DrawText($"{Runtime} · {MemoryMB} MB", r.MidX, r.Bottom - 6, SKTextAlign.Center, metaFont, metaPaint);

            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Position.X, p.Position.Y, 4, outPaint);
        }
    }
}
