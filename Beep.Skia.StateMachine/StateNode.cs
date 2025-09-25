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
        public string Title { get; set; } = "State";

        public StateNode()
        {
            Width = 140; Height = 64;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Outline;
            TextColor = MaterialColors.OnSurface;
            EnsurePortCounts(1, 2);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
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
