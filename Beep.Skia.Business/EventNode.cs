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
        private string _eventName = "Event";
        private EventType _eventType = EventType.Start;
        private string _triggerCondition = "";
        private bool _isTriggered = false;
        private DateTime? _triggerTime;
        public string EventName
        {
            get => _eventName;
            set
            {
                var v = value ?? string.Empty;
                if (_eventName != v)
                {
                    _eventName = v;
                    if (NodeProperties.TryGetValue("EventName", out var p)) p.ParameterCurrentValue = _eventName; else NodeProperties["EventName"] = new ParameterInfo { ParameterName = "EventName", ParameterType = typeof(string), DefaultParameterValue = _eventName, ParameterCurrentValue = _eventName, Description = "Event name" };
                    Name = _eventName;
                    InvalidateVisual();
                }
            }
        }
        public EventType EventType
        {
            get => _eventType;
            set
            {
                if (_eventType != value)
                {
                    _eventType = value;
                    if (NodeProperties.TryGetValue("EventType", out var p)) p.ParameterCurrentValue = _eventType; else NodeProperties["EventType"] = new ParameterInfo { ParameterName = "EventType", ParameterType = typeof(EventType), DefaultParameterValue = _eventType, ParameterCurrentValue = _eventType, Description = "Event type", Choices = Enum.GetNames(typeof(EventType)) };
                    InvalidateVisual();
                }
            }
        }
        public string TriggerCondition
        {
            get => _triggerCondition;
            set
            {
                var v = value ?? string.Empty;
                if (_triggerCondition != v)
                {
                    _triggerCondition = v;
                    if (NodeProperties.TryGetValue("TriggerCondition", out var p)) p.ParameterCurrentValue = _triggerCondition; else NodeProperties["TriggerCondition"] = new ParameterInfo { ParameterName = "TriggerCondition", ParameterType = typeof(string), DefaultParameterValue = _triggerCondition, ParameterCurrentValue = _triggerCondition, Description = "Trigger condition" };
                    InvalidateVisual();
                }
            }
        }
        public bool IsTriggered
        {
            get => _isTriggered;
            set
            {
                if (_isTriggered != value)
                {
                    _isTriggered = value;
                    if (NodeProperties.TryGetValue("IsTriggered", out var p)) p.ParameterCurrentValue = _isTriggered; else NodeProperties["IsTriggered"] = new ParameterInfo { ParameterName = "IsTriggered", ParameterType = typeof(bool), DefaultParameterValue = _isTriggered, ParameterCurrentValue = _isTriggered, Description = "Triggered state" };
                    InvalidateVisual();
                }
            }
        }
        public DateTime? TriggerTime
        {
            get => _triggerTime;
            set
            {
                if (_triggerTime != value)
                {
                    _triggerTime = value;
                    var iso = _triggerTime?.ToString("o");
                    if (NodeProperties.TryGetValue("TriggerTime", out var p)) p.ParameterCurrentValue = iso; else NodeProperties["TriggerTime"] = new ParameterInfo { ParameterName = "TriggerTime", ParameterType = typeof(string), DefaultParameterValue = iso, ParameterCurrentValue = iso, Description = "Trigger timestamp (ISO 8601)" };
                    InvalidateVisual();
                }
            }
        }

        public EventNode()
        {
            Width = 70;
            Height = 70;
            Name = "Event";
            ComponentType = BusinessComponentType.StartEvent; // Default to start event
            NodeProperties["EventName"] = new ParameterInfo { ParameterName = "EventName", ParameterType = typeof(string), DefaultParameterValue = _eventName, ParameterCurrentValue = _eventName, Description = "Event name" };
            NodeProperties["EventType"] = new ParameterInfo { ParameterName = "EventType", ParameterType = typeof(EventType), DefaultParameterValue = _eventType, ParameterCurrentValue = _eventType, Description = "Event type", Choices = Enum.GetNames(typeof(EventType)) };
            NodeProperties["TriggerCondition"] = new ParameterInfo { ParameterName = "TriggerCondition", ParameterType = typeof(string), DefaultParameterValue = _triggerCondition, ParameterCurrentValue = _triggerCondition, Description = "Trigger condition" };
            NodeProperties["IsTriggered"] = new ParameterInfo { ParameterName = "IsTriggered", ParameterType = typeof(bool), DefaultParameterValue = _isTriggered, ParameterCurrentValue = _isTriggered, Description = "Triggered state" };
            NodeProperties["TriggerTime"] = new ParameterInfo { ParameterName = "TriggerTime", ParameterType = typeof(string), DefaultParameterValue = _triggerTime?.ToString("o"), ParameterCurrentValue = _triggerTime?.ToString("o"), Description = "Trigger timestamp (ISO 8601)" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = IsTriggered ? MaterialColors.SecondaryContainer : BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = IsTriggered ? MaterialColors.Secondary : BorderColor,
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
                Color = IsTriggered ? MaterialColors.Secondary : BorderColor,
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

        protected override void LayoutPorts()
        {
            // Default event node: treat as circle (ellipse). One in/one out for generic events.
            EnsurePortCounts(1, 1);
            LayoutPortsOnEllipse(topInset: 4f, bottomInset: 4f, outwardOffset: 2f);
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