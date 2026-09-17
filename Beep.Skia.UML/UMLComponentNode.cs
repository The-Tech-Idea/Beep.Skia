using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML Component node — a modular part of a system with provided and required interfaces.
    /// Rendered as a rectangle with the component icon (two tabs) and a «component» stereotype.
    /// </summary>
    public class UMLComponentNode : UMLControl
    {
        private string _componentName = "Component";
        public string ComponentName
        {
            get => _componentName;
            set
            {
                var v = value ?? string.Empty;
                if (_componentName == v) return;
                _componentName = v;
                if (NodeProperties.TryGetValue("ComponentName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private string _providedInterfaces = string.Empty;
        public string ProvidedInterfaces
        {
            get => _providedInterfaces;
            set
            {
                var v = value ?? string.Empty;
                if (_providedInterfaces == v) return;
                _providedInterfaces = v;
                if (NodeProperties.TryGetValue("ProvidedInterfaces", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private string _requiredInterfaces = string.Empty;
        public string RequiredInterfaces
        {
            get => _requiredInterfaces;
            set
            {
                var v = value ?? string.Empty;
                if (_requiredInterfaces == v) return;
                _requiredInterfaces = v;
                if (NodeProperties.TryGetValue("RequiredInterfaces", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public UMLComponentNode()
        {
            Width = 170;
            Height = 96;
            Name = "Component";

            NodeProperties["ComponentName"] = new ParameterInfo { ParameterName = "ComponentName", ParameterType = typeof(string), DefaultParameterValue = _componentName, ParameterCurrentValue = _componentName, Description = "Component name" };
            NodeProperties["ProvidedInterfaces"] = new ParameterInfo { ParameterName = "ProvidedInterfaces", ParameterType = typeof(string), DefaultParameterValue = _providedInterfaces, ParameterCurrentValue = _providedInterfaces, Description = "Provided interfaces (comma separated)" };
            NodeProperties["RequiredInterfaces"] = new ParameterInfo { ParameterName = "RequiredInterfaces", ParameterType = typeof(string), DefaultParameterValue = _requiredInterfaces, ParameterCurrentValue = _requiredInterfaces, Description = "Required interfaces (comma separated)" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawRoundRect(rect, 4, 4, fill);
            canvas.DrawRoundRect(rect, 4, 4, stroke);

            // Component icon: two tabs on the left edge.
            const float tabWidth = 16f;
            const float tabHeight = 9f;
            var tab1 = new SKRect(X + 8f, Y + 18f, X + 8f + tabWidth, Y + 18f + tabHeight);
            var tab2 = new SKRect(X + 8f, Y + 34f, X + 8f + tabWidth, Y + 34f + tabHeight);
            canvas.DrawRect(tab1, fill);
            canvas.DrawRect(tab1, stroke);
            canvas.DrawRect(tab2, fill);
            canvas.DrawRect(tab2, stroke);

            using var stereotypeFont = new SKFont(SKTypeface.Default, 9);
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

            canvas.DrawText("«component»", X + Width / 2f, Y + 20f, SKTextAlign.Center, stereotypeFont, textPaint);
            canvas.DrawText(_componentName, X + Width / 2f, Y + 40f, SKTextAlign.Center, nameFont, textPaint);

            if (!string.IsNullOrWhiteSpace(_providedInterfaces))
                canvas.DrawText("provides: " + _providedInterfaces, X + 30f, Y + 62f, SKTextAlign.Left, stereotypeFont, textPaint);
            if (!string.IsNullOrWhiteSpace(_requiredInterfaces))
                canvas.DrawText("requires: " + _requiredInterfaces, X + 30f, Y + 78f, SKTextAlign.Left, stereotypeFont, textPaint);

            DrawConnectionPoints(canvas, context);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["ComponentName"] = _componentName;
            props["ProvidedInterfaces"] = _providedInterfaces;
            props["RequiredInterfaces"] = _requiredInterfaces;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("ComponentName", out var n) && n is string sn) ComponentName = sn;
            if (properties.TryGetValue("ProvidedInterfaces", out var p) && p is string sp) ProvidedInterfaces = sp;
            if (properties.TryGetValue("RequiredInterfaces", out var r) && r is string sr) RequiredInterfaces = sr;
        }
    }
}
