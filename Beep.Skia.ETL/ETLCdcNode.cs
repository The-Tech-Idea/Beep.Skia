using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.ETL
{
    public class ETLCdcNode : ETLControl
    {
        public enum CdcMethod { Timestamp, Version, FullRefresh, LogBased }

        private CdcMethod _method = CdcMethod.Timestamp;
        private string _trackingColumn = "ModifiedAt";
        private string _changeIndicator = "__CDC_Operation";

        public CdcMethod Method { get => _method; set { if (_method == value) return; _method = value; SetProp("Method", value, "CDC detection method", System.Enum.GetNames(typeof(CdcMethod))); InvalidateVisual(); } }
        public string TrackingColumn { get => _trackingColumn; set { var v = value ?? "ModifiedAt"; if (_trackingColumn == v) return; _trackingColumn = v; SetProp("TrackingColumn", v); InvalidateVisual(); } }
        public string ChangeIndicator { get => _changeIndicator; set { var v = value ?? "__CDC_Operation"; if (_changeIndicator == v) return; _changeIndicator = v; SetProp("ChangeIndicator", v); InvalidateVisual(); } }

        private void SetProp(string name, object val, string desc = null, string[] choices = null)
        {
            if (NodeProperties.TryGetValue(name, out var p) && p != null) p.ParameterCurrentValue = val;
            else NodeProperties[name] = new Model.ParameterInfo { ParameterName = name, ParameterType = val.GetType(), DefaultParameterValue = val, ParameterCurrentValue = val, Description = desc ?? name, Choices = choices };
        }

        public ETLCdcNode()
        {
            Width = 130; Height = 90; Name = "CDC";
            EnsurePortCounts(2, 1);
            SetProp("Method", _method, "CDC detection method", System.Enum.GetNames(typeof(CdcMethod)));
            SetProp("TrackingColumn", _trackingColumn, "Timestamp or version column");
            SetProp("ChangeIndicator", _changeIndicator, "Output column for I/U/D indicator");
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xFF, 0xF8, 0xE1), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xFF, 0xA0, 0x00), StrokeWidth = 2f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 8, 8, fill);
            canvas.DrawRoundRect(r, 8, 8, stroke);
            using var clockCircle = new SKPaint { Color = new SKColor(0xFF, 0xA0, 0x00), Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };
            canvas.DrawCircle(r.MidX, r.MidY - 8, 10, clockCircle);
            using var hand = new SKPaint { Color = new SKColor(0xFF, 0xA0, 0x00), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawLine(r.MidX, r.MidY - 8, r.MidX + 6, r.MidY - 8, hand);
            canvas.DrawLine(r.MidX, r.MidY - 8, r.MidX, r.MidY - 14, hand);
            using var font = new SKFont(SKTypeface.Default, 9);
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText("CDC", r.MidX, r.MidY + 12, SKTextAlign.Center, font, text);
            DrawPorts(canvas);
        }

        protected override void LayoutPorts() { LayoutPortsVerticalSegments(Height * 0.2f, Height * 0.2f); }
    }
}
