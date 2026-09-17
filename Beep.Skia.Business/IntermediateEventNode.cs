using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// BPMN 2.0 Intermediate Event node.
    /// Rendered as a double-ring circle with event type icon inside the inner ring.
    /// Supports Catch (incoming flow triggers event) and Throw (event triggers outgoing flow) semantics.
    /// </summary>
    public class IntermediateEventNode : BusinessControl
    {
        private string _label = "";
        private EventType _eventType = EventType.Timer;
        private EventPosition _eventPosition = EventPosition.IntermediateCatch;

        /// <summary>
        /// Event label
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (_label == v) return;
                _label = v;
                Name = _label;
                if (NodeProperties.TryGetValue("Label", out var p)) p.ParameterCurrentValue = _label;
                else NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Event label" };
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Event type
        /// </summary>
        public EventType EventType
        {
            get => _eventType;
            set
            {
                if (_eventType == value) return;
                _eventType = value;
                if (NodeProperties.TryGetValue("EventType", out var p)) p.ParameterCurrentValue = _eventType;
                else NodeProperties["EventType"] = new ParameterInfo { ParameterName = "EventType", ParameterType = typeof(EventType), DefaultParameterValue = _eventType, ParameterCurrentValue = _eventType, Description = "Event type", Choices = Enum.GetNames(typeof(EventType)) };
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Catch or Throw
        /// </summary>
        public EventPosition EventPosition
        {
            get => _eventPosition;
            set
            {
                if (_eventPosition == value) return;
                _eventPosition = value;
                if (NodeProperties.TryGetValue("EventPosition", out var p)) p.ParameterCurrentValue = _eventPosition;
                else NodeProperties["EventPosition"] = new ParameterInfo { ParameterName = "EventPosition", ParameterType = typeof(EventPosition), DefaultParameterValue = _eventPosition, ParameterCurrentValue = _eventPosition, Description = "Catch or Throw", Choices = Enum.GetNames(typeof(EventPosition)) };
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Event label
        /// </summary>
        public IntermediateEventNode()
        {
            Width = 60;
            Height = 60;
            Name = "IntermediateEvent";
            // Intermediate events participate in the flow: one input, one output.
            ComponentType = BusinessComponentType.Task;
            NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Event label" };
            NodeProperties["EventType"] = new ParameterInfo { ParameterName = "EventType", ParameterType = typeof(EventType), DefaultParameterValue = _eventType, ParameterCurrentValue = _eventType, Description = "Event type", Choices = Enum.GetNames(typeof(EventType)) };
            NodeProperties["EventPosition"] = new ParameterInfo { ParameterName = "EventPosition", ParameterType = typeof(EventPosition), DefaultParameterValue = _eventPosition, ParameterCurrentValue = _eventPosition, Description = "Catch or Throw", Choices = Enum.GetNames(typeof(EventPosition)) };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            float outerR = Math.Min(Width, Height) / 2f - 2f;
            float innerR = outerR * 0.75f;

            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var strokeThin = new SKPaint { Color = BorderColor, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
            using var strokeThick = new SKPaint { Color = _eventPosition == EventPosition.IntermediateThrow ? BorderColor : BorderColor, StrokeWidth = _eventPosition == EventPosition.IntermediateThrow ? 2.5f : 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };

            // Outer ring
            canvas.DrawCircle(cx, cy, outerR, fill);
            canvas.DrawCircle(cx, cy, outerR, strokeThin);
            // Inner ring
            canvas.DrawCircle(cx, cy, innerR, strokeThin);

            // Fill center if throw event
            if (_eventPosition == EventPosition.IntermediateThrow)
            {
                using var innerFill = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            }

            // Draw event type icon inside inner ring
            DrawEventIcon(canvas, cx, cy, innerR * 0.5f);
        }

        private void DrawEventIcon(SKCanvas canvas, float cx, float cy, float size)
        {
            using var iconPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };
            using var iconFill = new SKPaint
            {
                Color = BorderColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            switch (_eventType)
            {
                case EventType.Timer:
                    canvas.DrawCircle(cx, cy, size * 0.4f, iconPaint);
                    canvas.DrawLine(cx, cy, cx + size * 0.25f, cy, iconPaint);
                    canvas.DrawLine(cx, cy, cx, cy - size * 0.3f, iconPaint);
                    break;

                case EventType.Message:
                    using (var pathBuilder = new SKPathBuilder())
                    {
                        pathBuilder.MoveTo(cx - size * 0.5f, cy - size * 0.3f);
                        pathBuilder.LineTo(cx, cy + size * 0.3f);
                        pathBuilder.LineTo(cx + size * 0.5f, cy - size * 0.3f);
                        pathBuilder.Close();
                        using var path = pathBuilder.Detach();
                        canvas.DrawPath(path, iconPaint);
                    }
                    break;

                case EventType.Error:
                    canvas.DrawLine(cx, cy - size * 0.4f, cx, cy + size * 0.1f, iconPaint);
                    canvas.DrawLine(cx, cy - size * 0.4f, cx + size * 0.2f, cy - size * 0.2f, iconPaint);
                    canvas.DrawCircle(cx, cy + size * 0.3f, 1.5f, iconFill);
                    break;

                case EventType.Signal:
                    canvas.DrawLine(cx, cy - size * 0.4f, cx + size * 0.3f, cy - size * 0.1f, iconPaint);
                    canvas.DrawLine(cx, cy - size * 0.1f, cx + size * 0.3f, cy + size * 0.2f, iconPaint);
                    canvas.DrawLine(cx, cy + size * 0.2f, cx + size * 0.3f, cy + size * 0.5f, iconPaint);
                    using (var fill = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Fill, IsAntialias = true })
                        canvas.DrawCircle(cx, cy + size * 0.45f, 2f, fill);
                    break;

                case EventType.Conditional:
                    var cr = new SKRect(cx - size * 0.3f, cy - size * 0.3f, cx + size * 0.3f, cy + size * 0.3f);
                    canvas.DrawRoundRect(cr, 4, 4, iconPaint);
                    using (var textFont = new SKFont(SKTypeface.Default, size * 0.5f))
                    using (var textP = new SKPaint { Color = BorderColor, IsAntialias = true })
                        canvas.DrawText("?", cx, cy + size * 0.18f, SKTextAlign.Center, textFont, textP);
                    break;

                case EventType.Escalation:
                    canvas.DrawLine(cx, cy - size * 0.35f, cx, cy + size * 0.25f, iconPaint);
                    canvas.DrawLine(cx, cy - size * 0.35f, cx - size * 0.2f, cy - size * 0.1f, iconPaint);
                    canvas.DrawLine(cx, cy - size * 0.35f, cx + size * 0.2f, cy - size * 0.1f, iconPaint);
                    break;

                case EventType.Compensation:
                    canvas.DrawLine(cx + size * 0.3f, cy - size * 0.3f, cx - size * 0.3f, cy + size * 0.3f, iconPaint);
                    canvas.DrawLine(cx + size * 0.3f, cy - size * 0.3f, cx + size * 0.3f, cy - size * 0.15f, iconPaint);
                    canvas.DrawLine(cx + size * 0.3f, cy - size * 0.3f, cx + size * 0.15f, cy - size * 0.3f, iconPaint);
                    break;

                case EventType.Link:
                    canvas.DrawLine(cx - size * 0.4f, cy, cx + size * 0.2f, cy, iconPaint);
                    canvas.DrawLine(cx + size * 0.2f, cy - size * 0.2f, cx + size * 0.4f, cy, iconPaint);
                    canvas.DrawLine(cx + size * 0.2f, cy + size * 0.2f, cx + size * 0.4f, cy, iconPaint);
                    break;

                case EventType.Terminate:
                    canvas.DrawCircle(cx, cy, size * 0.35f, iconFill);
                    break;

                case EventType.Cancel:
                    canvas.DrawLine(cx - size * 0.25f, cy - size * 0.25f, cx + size * 0.25f, cy + size * 0.25f, iconPaint);
                    canvas.DrawLine(cx + size * 0.25f, cy - size * 0.25f, cx - size * 0.25f, cy + size * 0.25f, iconPaint);
                    break;

                case EventType.Multiple:
                    canvas.DrawCircle(cx - size * 0.2f, cy - size * 0.15f, size * 0.12f, iconPaint);
                    canvas.DrawCircle(cx + size * 0.2f, cy - size * 0.15f, size * 0.12f, iconPaint);
                    canvas.DrawCircle(cx, cy + size * 0.2f, size * 0.12f, iconPaint);
                    break;
            }
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(1, 1);
            LayoutPortsOnEllipse(topInset: 4f, bottomInset: 4f, outwardOffset: 2f);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrWhiteSpace(_label)) return;
            using var font = new SKFont(SKTypeface.Default, 8);
            using var paint = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(_label, X + Width / 2f, Y + Height + 12f, SKTextAlign.Center, font, paint);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["EventType"] = _eventType;
            props["EventPosition"] = _eventPosition;
            return props;
        }

        /// <summary>
        /// Gets or sets the set propperties.
        /// </summary>
        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("EventType", out var et) && et is EventType type) EventType = type;
            if (properties.TryGetValue("EventPosition", out var ep) && ep is EventPosition pos) EventPosition = pos;
        }
    }
}
