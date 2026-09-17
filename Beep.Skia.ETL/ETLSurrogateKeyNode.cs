using SkiaSharp;
using Beep.Skia.Components;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.ETL
{
    public class ETLSurrogateKeyNode : ETLControl
    {
        private string _keyColumn = "SurrogateKey";
        private long _startValue = 1;
        private long _increment = 1;

        /// <summary>
        /// Gets or sets the key column.
        /// </summary>
        public string KeyColumn { get => _keyColumn; set { var v = value ?? "SurrogateKey"; if (_keyColumn == v) return; _keyColumn = v; SetProp("KeyColumn", v); InvalidateVisual(); } }
        /// <summary>
        /// Gets or sets the start value.
        /// </summary>
        public long StartValue { get => _startValue; set { if (_startValue == value) return; _startValue = value; SetProp("StartValue", value); InvalidateVisual(); } }
        /// <summary>
        /// Gets or sets the increment.
        /// </summary>
        public long Increment { get => _increment; set { if (_increment == value) return; _increment = value; SetProp("Increment", value); InvalidateVisual(); } }

        private void SetProp(string name, object val, string desc = null)
        {
            if (NodeProperties.TryGetValue(name, out var p) && p != null) p.ParameterCurrentValue = val;
            else NodeProperties[name] = new ParameterInfo { ParameterName = name, ParameterType = val.GetType(), DefaultParameterValue = val, ParameterCurrentValue = val, Description = desc ?? name };
        }

        /// <summary>
        /// Surrogate key column name
        /// </summary>
        public ETLSurrogateKeyNode()
        {
            Width = 140; Height = 80; Name = "SurrogateKey";
            EnsurePortCounts(1, 1);
            SetProp("KeyColumn", _keyColumn, "Surrogate key column name");
            SetProp("StartValue", _startValue, "Starting key value");
            SetProp("Increment", _increment, "Increment per row");
        }

        protected override void DrawETLContent(SKCanvas canvas, DrawingContext context)
        {
            var r = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE3, 0xF2, 0xFD), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x1E, 0x88, 0xE5), StrokeWidth = 2f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawRoundRect(r, 8, 8, fill);
            canvas.DrawRoundRect(r, 8, 8, stroke);
            using var font = new SKFont(SKTypeface.Default, 9) { Embolden = true };
            using var text = new SKPaint { Color = new SKColor(0x33, 0x33, 0x33), IsAntialias = true };
            canvas.DrawText("SK", r.MidX, r.MidY - 3, SKTextAlign.Center, font, text);
            using var subFont = new SKFont(SKTypeface.Default, 7);
            using var subText = new SKPaint { Color = new SKColor(0x75, 0x75, 0x75), IsAntialias = true };
            canvas.DrawText("1,2,3...", r.MidX, r.MidY + 11, SKTextAlign.Center, subFont, subText);
            DrawPorts(canvas);
        }

        protected override void LayoutPorts() { LayoutPortsVerticalSegments(Height * 0.25f, Height * 0.25f); }
    }
}
