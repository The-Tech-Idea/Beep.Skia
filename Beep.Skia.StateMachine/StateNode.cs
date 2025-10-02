using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Regular state: rounded rectangle with optional title, one input and multiple outputs.
    /// </summary>
    public class StateNode : StateMachineControl
    {
        private string _title = "State";
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? string.Empty;
                if (_title == v) return;
                _title = v;
                if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title;
                InvalidateVisual();
            }
        }

        public StateNode()
        {
            Width = 140; Height = 64;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Outline;
            TextColor = MaterialColors.OnSurface;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 2);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "State title" };
        }

        protected override void LayoutPorts()
        {
            // Regular state: rounded rectangle with inputs on left, outputs on right
            LayoutPortsRightEdge(6f, 6f);
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 10, 10, fill);
            canvas.DrawRoundRect(rect, 10, 10, stroke);

            using var font = new SKFont(SKTypeface.Default, 13) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);

            DrawConnectionPoints(canvas);
        }
    }
}
