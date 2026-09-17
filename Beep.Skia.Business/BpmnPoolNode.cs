using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Business
{
    /// <summary>
    /// BPMN Pool (participant) container. Drawn as a rounded rectangle with a vertical
    /// title bar on the left. Nodes whose center lies inside the pool belong to it.
    /// </summary>
    public class BpmnPoolNode : BusinessControl
    {
        private string _poolName = "Pool";
        public string PoolName
        {
            get => _poolName;
            set
            {
                var v = value ?? string.Empty;
                if (_poolName == v) return;
                _poolName = v;
                Name = _poolName;
                if (NodeProperties.TryGetValue("PoolName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public BpmnPoolNode()
        {
            Width = 460;
            Height = 240;
            Name = "Pool";
            BackgroundColor = new SKColor(0xFA, 0xFA, 0xFA);
            BorderColor = new SKColor(0x90, 0x90, 0x90);
            EnsurePortCounts(0, 0);
            NodeProperties["PoolName"] = new ParameterInfo { ParameterName = "PoolName", ParameterType = typeof(string), DefaultParameterValue = _poolName, ParameterCurrentValue = _poolName, Description = "Pool / participant name" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawRoundRect(rect, 4, 4, fill);
            canvas.DrawRoundRect(rect, 4, 4, stroke);

            // Vertical title bar on the left (BPMN pool style).
            const float barWidth = 26f;
            var bar = new SKRect(X, Y, X + barWidth, Y + Height);
            using var barFill = new SKPaint { Color = BorderColor.WithAlpha(40), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRect(bar, barFill);
            canvas.DrawLine(bar.Right, bar.Top, bar.Right, bar.Bottom, stroke);

            using var font = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.Save();
            canvas.RotateDegrees(-90, X + barWidth / 2f, Y + Height / 2f);
            canvas.DrawText(_poolName, X + barWidth / 2f, Y + Height / 2f + 4f, SKTextAlign.Center, font, textPaint);
            canvas.Restore();
        }
    }
}
