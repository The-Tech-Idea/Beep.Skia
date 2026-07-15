using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML Package Node — a tabbed folder shape representing a namespace or module.
    /// </summary>
    public class UMLPackageNode : UMLControl
    {
        private string _packageName = "Package";
        public string PackageName
        {
            get => _packageName;
            set
            {
                var v = value ?? string.Empty;
                if (_packageName == v) return;
                _packageName = v;
                if (NodeProperties.TryGetValue("PackageName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private float _tabWidth = 60f;
        private float _tabHeight = 20f;

        public UMLPackageNode()
        {
            Width = 200;
            Height = 140;
            Name = "Package";
            BackgroundColor = new SKColor(0xFA, 0xFA, 0xFA);
            NodeProperties["PackageName"] = new ParameterInfo { ParameterName = "PackageName", ParameterType = typeof(string), DefaultParameterValue = _packageName, ParameterCurrentValue = _packageName, Description = "Package name" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, StrokeWidth = BorderThickness, Style = SKPaintStyle.Stroke, IsAntialias = true };

            // Tab (small rectangle at top-left)
            var tab = new SKRect(X, Y, X + _tabWidth, Y + _tabHeight);
            canvas.DrawRect(tab, fill);
            canvas.DrawRect(new SKRect(tab.Left, tab.Bottom, tab.Left + 1, tab.Bottom), stroke);
            canvas.DrawRect(new SKRect(tab.Left, tab.Top, tab.Right, tab.Top + 1), stroke);
            canvas.DrawRect(new SKRect(tab.Right - 1, tab.Top, tab.Right, tab.Bottom), stroke);

            // Main body (below tab)
            var body = new SKRect(X, Y + _tabHeight, X + Width, Y + Height);
            canvas.DrawRect(body, fill);
            canvas.DrawRect(body, stroke);
            // Don't draw top-left segment of body (under the tab)
            canvas.DrawLine(body.Left, body.Top, tab.Left, body.Top, stroke);
            canvas.DrawLine(tab.Right, body.Top, body.Right, body.Top, stroke);

            // Package name in the tab
            using var font = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(_packageName, tab.Left + 6, tab.Top + _tabHeight / 2f + 4, font, text);

            // Stereotype if set
            if (!string.IsNullOrWhiteSpace(Stereotype))
            {
                using var smallFont = new SKFont(SKTypeface.Default, 8);
                using var smallText = new SKPaint { Color = BorderColor, IsAntialias = true };
                canvas.DrawText(Stereotype, body.Left + 6, body.Top + 14, smallFont, smallText);
            }

            DrawConnectionPoints(canvas);
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(1, 1);
            LayoutPortsOnEllipse(4, 4, 2);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["PackageName"] = _packageName;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("PackageName", out var n) && n is string sn) PackageName = sn;
        }
    }
}
