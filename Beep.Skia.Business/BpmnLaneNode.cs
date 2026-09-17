using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Business
{
    /// <summary>
    /// BPMN Lane container (a subdivision of a pool). Drawn as a rectangle with a
    /// header band at the top. Nodes whose center lies inside the lane belong to it.
    /// </summary>
    public class BpmnLaneNode : BusinessControl
    {
        private string _laneName = "Lane";
        /// <summary>
        /// Gets or sets the lane name.
        /// </summary>
        public string LaneName
        {
            get => _laneName;
            set
            {
                var v = value ?? string.Empty;
                if (_laneName == v) return;
                _laneName = v;
                Name = _laneName;
                if (NodeProperties.TryGetValue("LaneName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Lane name
        /// </summary>
        public BpmnLaneNode()
        {
            Width = 400;
            Height = 110;
            Name = "Lane";
            BackgroundColor = new SKColor(0xFD, 0xFD, 0xFD);
            BorderColor = new SKColor(0xB0, 0xB0, 0xB0);
            EnsurePortCounts(0, 0);
            NodeProperties["LaneName"] = new ParameterInfo { ParameterName = "LaneName", ParameterType = typeof(string), DefaultParameterValue = _laneName, ParameterCurrentValue = _laneName, Description = "Lane name" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawRect(rect, fill);
            canvas.DrawRect(rect, stroke);

            const float headerHeight = 22f;
            var header = new SKRect(X, Y, X + Width, Y + headerHeight);
            using var headerFill = new SKPaint { Color = BorderColor.WithAlpha(35), Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRect(header, headerFill);
            canvas.DrawLine(header.Left, header.Bottom, header.Right, header.Bottom, stroke);

            using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(_laneName, X + 8f, Y + 15f, SKTextAlign.Left, font, textPaint);
        }
    }
}
