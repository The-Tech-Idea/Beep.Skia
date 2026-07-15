using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Cloud
{
    /// <summary>
    /// AWS EC2 / Azure VM / GCP Compute Engine — virtual machine instance.
    /// </summary>
    public class VirtualMachineNode : CloudControl
    {
        private string _instanceType = "t3.medium";
        private string _os = "Linux";
        public string InstanceType { get => _instanceType; set { var v = value ?? ""; if (_instanceType == v) return; _instanceType = v; SetProp("InstanceType", v); InvalidateVisual(); } }
        public string OperatingSystem { get => _os; set { var v = value ?? ""; if (_os == v) return; _os = v; SetProp("OperatingSystem", v); InvalidateVisual(); } }

        private void SetProp(string n, object v, string d = null) { if (NodeProperties.TryGetValue(n, out var p) && p != null) p.ParameterCurrentValue = v; else NodeProperties[n] = new ParameterInfo { ParameterName = n, ParameterType = v.GetType(), DefaultParameterValue = v, ParameterCurrentValue = v, Description = d ?? n }; }

        public VirtualMachineNode() { Width = 120; Height = 70; Name = "VM"; EnsurePortCounts(1, 1); SetProp("InstanceType", _instanceType); SetProp("OperatingSystem", _os); }
        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext ctx)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x1E, 0x88, 0xE5), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill); canvas.DrawRoundRect(r, 6, 6, stroke);
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText("VM", r.MidX, r.MidY - 4, SKTextAlign.Center, font, text);
            using var sf = new SKFont(SKTypeface.Default, 7);
            canvas.DrawText(_instanceType, r.MidX, r.MidY + 10, SKTextAlign.Center, sf, text);
        }
        protected override void LayoutPorts() { LayoutPortsVerticalSegments(6, 6); }
    }

    /// <summary>
    /// AWS S3 / Azure Blob / GCP Cloud Storage — object storage bucket.
    /// </summary>
    public class ObjectStorageNode : CloudControl
    {
        private string _storageClass = "Standard";
        public string StorageClass { get => _storageClass; set { var v = value ?? ""; if (_storageClass == v) return; _storageClass = v; SetProp("StorageClass", v); InvalidateVisual(); } }
        private void SetProp(string n, object v, string d = null) { if (NodeProperties.TryGetValue(n, out var p) && p != null) p.ParameterCurrentValue = v; else NodeProperties[n] = new ParameterInfo { ParameterName = n, ParameterType = v.GetType(), DefaultParameterValue = v, ParameterCurrentValue = v, Description = d ?? n }; }
        public ObjectStorageNode() { Width = 120; Height = 70; Name = "Storage"; EnsurePortCounts(1, 1); SetProp("StorageClass", _storageClass); }
        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext ctx)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE8, 0xF5, 0xE9), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x4C, 0xAF, 0x50), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill); canvas.DrawRoundRect(r, 6, 6, stroke);
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText(Name, r.MidX, r.MidY, SKTextAlign.Center, font, text);
        }
        protected override void LayoutPorts() { LayoutPortsVerticalSegments(6, 6); }
    }

    /// <summary>
    /// AWS Lambda / Azure Functions / GCP Cloud Functions — serverless compute.
    /// </summary>
    public class ServerlessFunctionNode : CloudControl
    {
        private string _runtime = "dotnet8";
        private int _timeoutSec = 60;
        public string Runtime { get => _runtime; set { var v = value ?? ""; if (_runtime == v) return; _runtime = v; SetProp("Runtime", v); InvalidateVisual(); } }
        public int TimeoutSec { get => _timeoutSec; set { var v = Math.Max(1, value); if (_timeoutSec == v) return; _timeoutSec = v; SetProp("TimeoutSec", v); InvalidateVisual(); } }
        private void SetProp(string n, object v, string d = null) { if (NodeProperties.TryGetValue(n, out var p) && p != null) p.ParameterCurrentValue = v; else NodeProperties[n] = new ParameterInfo { ParameterName = n, ParameterType = v.GetType(), DefaultParameterValue = v, ParameterCurrentValue = v, Description = d ?? n }; }
        public ServerlessFunctionNode() { Width = 130; Height = 70; Name = "Function"; EnsurePortCounts(1, 1); SetProp("Runtime", _runtime); SetProp("TimeoutSec", _timeoutSec); }
        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext ctx)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF3, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFF, 0x98, 0x00), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill); canvas.DrawRoundRect(r, 6, 6, stroke);
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText("\u03BB", r.MidX - 8, r.MidY + 6, SKTextAlign.Center, font, text);
            canvas.DrawText(Name, r.MidX + 10, r.MidY + 6, font, text);
        }
        protected override void LayoutPorts() { LayoutPortsVerticalSegments(6, 6); }
    }

    /// <summary>
    /// AWS RDS / Azure SQL / GCP Cloud SQL — managed database service.
    /// </summary>
    public class ManagedDatabaseNode : CloudControl
    {
        private string _engine = "PostgreSQL";
        private string _version = "15";
        public string Engine { get => _engine; set { var v = value ?? ""; if (_engine == v) return; _engine = v; SetProp("Engine", v); InvalidateVisual(); } }
        public string Version { get => _version; set { var v = value ?? ""; if (_version == v) return; _version = v; SetProp("Version", v); InvalidateVisual(); } }
        private void SetProp(string n, object v, string d = null) { if (NodeProperties.TryGetValue(n, out var p) && p != null) p.ParameterCurrentValue = v; else NodeProperties[n] = new ParameterInfo { ParameterName = n, ParameterType = v.GetType(), DefaultParameterValue = v, ParameterCurrentValue = v, Description = d ?? n }; }
        public ManagedDatabaseNode() { Width = 130; Height = 75; Name = "RDS"; EnsurePortCounts(1, 1); SetProp("Engine", _engine); SetProp("Version", _version); }
        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext ctx)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE8, 0xE0, 0xF7), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x7B, 0x1F, 0xA2), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill); canvas.DrawRoundRect(r, 6, 6, stroke);
            // Cylinder icon
            using var icon = new SKPaint { Color = new SKColor(0x7B, 0x1F, 0xA2), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            float cx = r.MidX, cy = r.MidY - 8;
            canvas.DrawOval(new SKRect(cx - 12, cy - 4, cx + 12, cy + 4), icon);
            canvas.DrawLine(cx - 12, cy, cx - 12, cy + 10, icon);
            canvas.DrawLine(cx + 12, cy, cx + 12, cy + 10, icon);
            canvas.DrawOval(new SKRect(cx - 12, cy + 6, cx + 12, cy + 14), icon);
            using var font = new SKFont(SKTypeface.Default, 8);
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText($"DB: {_engine}", cx, r.MidY + 18, SKTextAlign.Center, font, text);
        }
        protected override void LayoutPorts() { LayoutPortsVerticalSegments(6, 6); }
    }

    /// <summary>
    /// AWS API Gateway / Azure API Management — API gateway.
    /// </summary>
    public class ApiGatewayNode : CloudControl
    {
        private string _stage = "prod";
        public string Stage { get => _stage; set { var v = value ?? ""; if (_stage == v) return; _stage = v; SetProp("Stage", v); InvalidateVisual(); } }
        private void SetProp(string n, object v, string d = null) { if (NodeProperties.TryGetValue(n, out var p) && p != null) p.ParameterCurrentValue = v; else NodeProperties[n] = new ParameterInfo { ParameterName = n, ParameterType = v.GetType(), DefaultParameterValue = v, ParameterCurrentValue = v, Description = d ?? n }; }
        public ApiGatewayNode() { Width = 130; Height = 65; Name = "API Gateway"; EnsurePortCounts(2, 2); SetProp("Stage", _stage); }
        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext ctx)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFD, 0xE0, 0xE0), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xE5, 0x39, 0x35), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 6, 6, fill); canvas.DrawRoundRect(r, 6, 6, stroke);
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText("API", r.MidX, r.MidY + 4, SKTextAlign.Center, font, text);
        }
        protected override void LayoutPorts() { LayoutPortsVerticalSegments(6, 6); }
    }

    /// <summary>
    /// VPC / VNet — virtual network with CIDR.
    /// </summary>
    public class VirtualNetworkNode : CloudControl
    {
        private string _cidr = "10.0.0.0/16";
        public string Cidr { get => _cidr; set { var v = value ?? ""; if (_cidr == v) return; _cidr = v; SetProp("Cidr", v); InvalidateVisual(); } }
        private void SetProp(string n, object v, string d = null) { if (NodeProperties.TryGetValue(n, out var p) && p != null) p.ParameterCurrentValue = v; else NodeProperties[n] = new ParameterInfo { ParameterName = n, ParameterType = v.GetType(), DefaultParameterValue = v, ParameterCurrentValue = v, Description = d ?? n }; }
        public VirtualNetworkNode() { Width = 240; Height = 160; Name = "VPC"; EnsurePortCounts(0, 0); }
        protected override void DrawCloudContent(SKCanvas canvas, DrawingContext ctx)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xF5, 0xF5, 0xF5).WithAlpha(80), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x45, 0x55, 0xA0), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 6f, 3f }, 0) };
            canvas.DrawRoundRect(r, 6, 6, fill); canvas.DrawRoundRect(r, 6, 6, stroke);
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var text = new SKPaint { Color = new SKColor(0x45, 0x55, 0xA0), IsAntialias = true };
            canvas.DrawText("VPC: " + _cidr, r.Left + 8, r.Top + 16, font, text);
        }
        protected override void LayoutPorts() { }
    }
}
