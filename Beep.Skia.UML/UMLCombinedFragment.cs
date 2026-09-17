using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML combined fragment (sequence diagram) — a frame with an operator tab
    /// (alt, opt, loop, par, break, critical, neg, assert, ignore, consider) and an optional guard.
    /// </summary>
    public class UMLCombinedFragment : UMLControl
    {
        /// <summary>Supported interaction operators.</summary>
        public static readonly string[] Operators =
        {
            "alt", "opt", "loop", "par", "break", "critical", "neg", "assert", "ignore", "consider"
        };

        private string _operator = "alt";
        /// <summary>
        /// Gets or sets the operator.
        /// </summary>
        public string Operator
        {
            get => _operator;
            set
            {
                var v = (value ?? string.Empty).Trim().ToLowerInvariant();
                if (!Operators.Contains(v)) v = "alt";
                if (_operator == v) return;
                _operator = v;
                if (NodeProperties.TryGetValue("Operator", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private string _guardCondition = string.Empty;
        /// <summary>
        /// Gets or sets the guard condition.
        /// </summary>
        public string GuardCondition
        {
            get => _guardCondition;
            set
            {
                var v = value ?? string.Empty;
                if (_guardCondition == v) return;
                _guardCondition = v;
                if (NodeProperties.TryGetValue("GuardCondition", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Interaction operator
        /// </summary>
        public UMLCombinedFragment()
        {
            Width = 320;
            Height = 180;
            Name = "CombinedFragment";
            BackgroundColor = new SKColor(0xFF, 0xFF, 0xFF, 0x20);
            BorderColor = new SKColor(0x42, 0x42, 0x42);

            NodeProperties["Operator"] = new ParameterInfo { ParameterName = "Operator", ParameterType = typeof(string), DefaultParameterValue = _operator, ParameterCurrentValue = _operator, Description = "Interaction operator", Choices = Operators };
            NodeProperties["GuardCondition"] = new ParameterInfo { ParameterName = "GuardCondition", ParameterType = typeof(string), DefaultParameterValue = _guardCondition, ParameterCurrentValue = _guardCondition, Description = "Optional guard, e.g., [x > 0]" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            canvas.DrawRect(rect, fill);
            canvas.DrawRect(rect, stroke);

            // Operator tab (pentagon) at the top-left.
            const float tabWidth = 52f;
            const float tabHeight = 20f;
            using var tabPathBuilder = new SKPathBuilder();
            tabPathBuilder.MoveTo(X, Y);
            tabPathBuilder.LineTo(X + tabWidth - 8f, Y);
            tabPathBuilder.LineTo(X + tabWidth, Y + 8f);
            tabPathBuilder.LineTo(X + tabWidth, Y + tabHeight);
            tabPathBuilder.LineTo(X, Y + tabHeight);
            tabPathBuilder.Close();
            using var tabPath = tabPathBuilder.Detach();
            canvas.DrawPath(tabPath, fill);
            canvas.DrawPath(tabPath, stroke);

            using var operatorFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var guardFont = new SKFont(SKTypeface.Default, 10);
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

            canvas.DrawText(_operator, X + tabWidth / 2f, Y + 14f, SKTextAlign.Center, operatorFont, textPaint);

            if (!string.IsNullOrWhiteSpace(_guardCondition))
            {
                var guard = _guardCondition.Trim();
                if (!guard.StartsWith("[")) guard = "[" + guard + "]";
                canvas.DrawText(guard, X + tabWidth + 8f, Y + 14f, SKTextAlign.Left, guardFont, textPaint);
            }

            // alt/par frames get a dashed operand divider.
            if (_operator == "alt" || _operator == "par")
            {
                using var divider = new SKPaint
                {
                    Color = BorderColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 6f, 4f }, 0)
                };
                float midY = Y + tabHeight + (Height - tabHeight) / 2f;
                canvas.DrawLine(X, midY, X + Width, midY, divider);
            }
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["Operator"] = _operator;
            props["GuardCondition"] = _guardCondition;
            return props;
        }

        /// <summary>
        /// Gets or sets the set propperties.
        /// </summary>
        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("Operator", out var o) && o is string so) Operator = so;
            if (properties.TryGetValue("GuardCondition", out var g) && g is string sg) GuardCondition = sg;
        }
    }
}
