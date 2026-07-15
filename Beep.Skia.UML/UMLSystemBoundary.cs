using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML System Boundary — a rectangle that groups related use cases.
    /// Rendered with a dashed border and optional title at the top-left.
    /// </summary>
    public class UMLSystemBoundary : UMLControl
    {
        private string _systemName = "System";
        public string SystemName
        {
            get => _systemName;
            set
            {
                var v = value ?? string.Empty;
                if (_systemName == v) return;
                _systemName = v;
                if (NodeProperties.TryGetValue("SystemName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public UMLSystemBoundary()
        {
            Width = 300;
            Height = 200;
            BackgroundColor = new SKColor(0xF5, 0xF5, 0xF5);
            BorderColor = new SKColor(0x75, 0x75, 0x75);
            Name = "SystemBoundary";
            EnsurePortCounts(0, 0);
            NodeProperties["SystemName"] = new ParameterInfo { ParameterName = "SystemName", ParameterType = typeof(string), DefaultParameterValue = _systemName, ParameterCurrentValue = _systemName, Description = "System or subsystem name" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var r = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor.WithAlpha(60), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var dashed = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 1.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 8f, 4f }, 0)
            };

            canvas.DrawRoundRect(r, 4, 4, fill);
            canvas.DrawRoundRect(r, 4, 4, dashed);

            // Title at top-left
            if (!string.IsNullOrWhiteSpace(_systemName))
            {
                using var font = new SKFont(SKTypeface.Default, 11) { Embolden = true };
                using var text = new SKPaint { Color = BorderColor, IsAntialias = true };
                canvas.DrawText(_systemName, X + 8, Y + 16, font, text);
            }
        }

        protected override void LayoutPorts() { EnsurePortCounts(0, 0); }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["SystemName"] = _systemName;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("SystemName", out var n) && n is string sn) SystemName = sn;
        }
    }
}
