using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML Activity Node — a rounded rectangle representing an action in an activity diagram.
    /// </summary>
    public class UMLActivityNode : UMLControl
    {
        private string _actionName = "Action";
        public string ActionName
        {
            get => _actionName;
            set
            {
                var v = value ?? string.Empty;
                if (_actionName == v) return;
                _actionName = v;
                DisplayText = v;
                if (NodeProperties.TryGetValue("ActionName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private ActivityNodeType _actionType = ActivityNodeType.Action;
        public ActivityNodeType ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType == value) return;
                _actionType = value;
                if (NodeProperties.TryGetValue("ActionType", out var pi)) pi.ParameterCurrentValue = value;
                InvalidateVisual();
            }
        }

        public UMLActivityNode()
        {
            Width = 160;
            Height = 60;
            Name = "ActivityAction";
            NodeProperties["ActionName"] = new ParameterInfo { ParameterName = "ActionName", ParameterType = typeof(string), DefaultParameterValue = _actionName, ParameterCurrentValue = _actionName, Description = "Action name" };
            NodeProperties["ActionType"] = new ParameterInfo { ParameterName = "ActionType", ParameterType = typeof(ActivityNodeType), DefaultParameterValue = _actionType, ParameterCurrentValue = _actionType, Description = "Action type", Choices = Enum.GetNames(typeof(ActivityNodeType)) };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            float radius = _actionType == ActivityNodeType.Initial || _actionType == ActivityNodeType.Final
                ? Math.Min(Width, Height) / 2f : 12f;

            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };

            canvas.DrawRoundRect(r, radius, radius, fill);

            if (_actionType == ActivityNodeType.Final)
            {
                canvas.DrawRoundRect(r, radius, radius, stroke);
                // Inner filled circle for final
                float inset = 5f;
                var inner = new SKRect(r.Left + inset, r.Top + inset, r.Right - inset, r.Bottom - inset);
                using var innerFill = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRoundRect(inner, radius - inset, radius - inset, innerFill);
            }
            else if (_actionType == ActivityNodeType.Initial)
            {
                using var innerFill = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRoundRect(r, radius, radius, innerFill);
            }
            else
            {
                canvas.DrawRoundRect(r, radius, radius, stroke);
            }

            if (_actionType == ActivityNodeType.Action || _actionType == ActivityNodeType.Object)
            {
                using var font = new SKFont(SKTypeface.Default, 11);
                using var text = new SKPaint { Color = TextColor, IsAntialias = true };
                canvas.DrawText(_actionName, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);
            }

            DrawConnectionPoints(canvas, context);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["ActionName"] = _actionName;
            props["ActionType"] = _actionType;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("ActionName", out var n) && n is string sn) ActionName = sn;
            if (properties.TryGetValue("ActionType", out var t) && t is ActivityNodeType at) ActionType = at;
        }
    }

    public enum ActivityNodeType
    {
        Action,
        Initial,
        Final,
        Object,
        Fork,
        Join,
        Decision,
        Merge
    }
}
