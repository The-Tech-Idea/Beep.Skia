using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML sequence activation bar — a narrow vertical rectangle placed on a lifeline
    /// to represent the period during which an object is active.
    /// </summary>
    public class UMLActivationBar : UMLControl
    {
        private string _label = string.Empty;
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (_label == v) return;
                _label = v;
                if (NodeProperties.TryGetValue("Label", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Optional activation label
        /// </summary>
        public UMLActivationBar()
        {
            Width = 12;
            Height = 90;
            Name = "Activation";
            BackgroundColor = SKColors.White;
            BorderColor = SKColors.Black;
            NodeProperties["Label"] = new ParameterInfo { ParameterName = "Label", ParameterType = typeof(string), DefaultParameterValue = _label, ParameterCurrentValue = _label, Description = "Optional activation label" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawRect(rect, fill);
            canvas.DrawRect(rect, stroke);

            if (!string.IsNullOrWhiteSpace(_label))
            {
                using var font = new SKFont(SKTypeface.Default, 9);
                using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
                canvas.Save();
                canvas.RotateDegrees(-90, rect.MidX, rect.MidY);
                canvas.DrawText(_label, rect.MidX, rect.MidY + 3f, SKTextAlign.Center, font, textPaint);
                canvas.Restore();
            }
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["Label"] = _label;
            return props;
        }

        /// <summary>
        /// Gets or sets the set propperties.
        /// </summary>
        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("Label", out var v) && v is string s) Label = s;
        }
    }
}
