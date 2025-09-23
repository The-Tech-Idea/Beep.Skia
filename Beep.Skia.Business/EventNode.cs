using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a workflow event node that can trigger or respond to events.
    /// Displayed as a circle with event-specific icons and timing information.
    /// </summary>
    public class EventNode : BusinessControl
    {
        public string EventName { get; set; } = "Event";
        public EventType EventType { get; set; } = EventType.Start;
        public string TriggerCondition { get; set; } = "";
        public bool IsTriggered { get; set; } = false;
        public DateTime? TriggerTime { get; set; }

        public EventNode()
        {
            Width = 70;
            Height = 70;
            Name = "Event";
            ComponentType = BusinessComponentType.StartEvent; // Default to start event
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = IsTriggered ? SKColors.LightGreen : BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = IsTriggered ? 3 : BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - 2;

            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            canvas.DrawCircle(centerX, centerY, radius, borderPaint);

            // Draw event-specific icon
            DrawEventIcon(canvas, centerX, centerY, radius * 0.6f);
        }

        private void DrawEventIcon(SKCanvas canvas, float centerX, float centerY, float iconSize)
        {
            using var iconPaint = new SKPaint
            {
                Color = IsTriggered ? SKColors.DarkGreen : BorderColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            switch (EventType)
            {
                case EventType.Start:
                    // Draw play triangle
                    using (var path = new SKPath())
                    {
                        path.MoveTo(centerX - iconSize * 0.4f, centerY - iconSize * 0.5f);
                        path.LineTo(centerX + iconSize * 0.4f, centerY);
                        path.LineTo(centerX - iconSize * 0.4f, centerY + iconSize * 0.5f);
                        path.Close();
                        canvas.DrawPath(path, iconPaint);
                    }
                    break;

                case EventType.End:
                    // Draw stop square
                    var squareSize = iconSize * 0.6f;
                    var squareRect = new SKRect(
                        centerX - squareSize, centerY - squareSize,
                        centerX + squareSize, centerY + squareSize);
                    canvas.DrawRect(squareRect, iconPaint);
                    break;

                case EventType.Timer:
                    // Draw clock face
                    canvas.DrawCircle(centerX, centerY, iconSize * 0.4f, iconPaint);
                    // Clock hands
                    canvas.DrawLine(centerX, centerY, centerX + iconSize * 0.2f, centerY, iconPaint);
                    canvas.DrawLine(centerX, centerY, centerX, centerY - iconSize * 0.3f, iconPaint);
                    break;

                case EventType.Message:
                    // Draw envelope
                    using (var path = new SKPath())
                    {
                        path.MoveTo(centerX - iconSize * 0.5f, centerY - iconSize * 0.3f);
                        path.LineTo(centerX, centerY + iconSize * 0.3f);
                        path.LineTo(centerX + iconSize * 0.5f, centerY - iconSize * 0.3f);
                        path.LineTo(centerX - iconSize * 0.5f, centerY - iconSize * 0.3f);
                        path.MoveTo(centerX - iconSize * 0.5f, centerY - iconSize * 0.3f);
                        path.LineTo(centerX - iconSize * 0.5f, centerY + iconSize * 0.1f);
                        path.LineTo(centerX, centerY + iconSize * 0.3f);
                        canvas.DrawPath(path, iconPaint);
                    }
                    break;

                case EventType.Error:
                    // Draw exclamation mark
                    canvas.DrawLine(centerX, centerY - iconSize * 0.4f, centerX, centerY + iconSize * 0.2f, iconPaint);
                    canvas.DrawCircle(centerX, centerY + iconSize * 0.4f, 2, iconPaint);
                    break;
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(EventName))
                return;

            using var font = new SKFont(SKTypeface.Default, 9);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 12;

            canvas.DrawText(EventName, centerX, textY, SKTextAlign.Center, font, paint);
        }
    }
}