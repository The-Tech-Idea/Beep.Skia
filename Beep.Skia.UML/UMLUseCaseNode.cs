using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML Use Case node — an oval representing a user goal or system function.
    /// </summary>
    public class UMLUseCaseNode : UMLControl
    {
        private string _useCaseName = "UseCase";
        public string UseCaseName
        {
            get => _useCaseName;
            set
            {
                var v = value ?? string.Empty;
                if (_useCaseName == v) return;
                _useCaseName = v;
                DisplayText = v;
                if (NodeProperties.TryGetValue("UseCaseName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public UMLUseCaseNode()
        {
            Width = 140;
            Height = 70;
            Name = "UseCase";
            NodeProperties["UseCaseName"] = new ParameterInfo { ParameterName = "UseCaseName", ParameterType = typeof(string), DefaultParameterValue = _useCaseName, ParameterCurrentValue = _useCaseName, Description = "Use case name" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };
            canvas.DrawOval(r, fill);
            canvas.DrawOval(r, stroke);

            using var font = new SKFont(SKTypeface.Default, 12);
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(_useCaseName, X + Width / 2f, Y + Height / 2f + 4, SKTextAlign.Center, font, text);

            DrawConnectionPoints(canvas, context);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["UseCaseName"] = _useCaseName;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("UseCaseName", out var n) && n is string sn) UseCaseName = sn;
        }
    }
}
