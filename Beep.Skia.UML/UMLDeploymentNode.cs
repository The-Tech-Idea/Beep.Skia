using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML Deployment node — a physical or execution environment (device / execution environment).
    /// Rendered as a 3D box with the «node» stereotype.
    /// </summary>
    public class UMLDeploymentNode : UMLControl
    {
        private string _nodeName = "Node";
        public string NodeName
        {
            get => _nodeName;
            set
            {
                var v = value ?? string.Empty;
                if (_nodeName == v) return;
                _nodeName = v;
                if (NodeProperties.TryGetValue("NodeName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private string _nodeType = "Device";
        public string NodeType
        {
            get => _nodeType;
            set
            {
                var v = string.IsNullOrWhiteSpace(value) ? "Device" : value;
                if (_nodeType == v) return;
                _nodeType = v;
                if (NodeProperties.TryGetValue("NodeType", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public UMLDeploymentNode()
        {
            Width = 170;
            Height = 100;
            Name = "DeploymentNode";

            NodeProperties["NodeName"] = new ParameterInfo { ParameterName = "NodeName", ParameterType = typeof(string), DefaultParameterValue = _nodeName, ParameterCurrentValue = _nodeName, Description = "Node name" };
            NodeProperties["NodeType"] = new ParameterInfo { ParameterName = "NodeType", ParameterType = typeof(string), DefaultParameterValue = _nodeType, ParameterCurrentValue = _nodeType, Description = "Device or ExecutionEnvironment", Choices = new[] { "Device", "ExecutionEnvironment" } };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            const float depth = 14f;
            var front = new SKRect(X, Y + depth, X + Width - depth, Y + Height);
            var topBuilder = new SKPathBuilder();
            topBuilder.MoveTo(X, Y + depth);
            topBuilder.LineTo(X + depth, Y);
            topBuilder.LineTo(X + Width, Y);
            topBuilder.LineTo(X + Width - depth, Y + depth);
            topBuilder.Close();

            var sideBuilder = new SKPathBuilder();
            sideBuilder.MoveTo(X + Width - depth, Y + depth);
            sideBuilder.LineTo(X + Width, Y);
            sideBuilder.LineTo(X + Width, Y + Height - depth);
            sideBuilder.LineTo(X + Width - depth, Y + Height);
            sideBuilder.Close();

            using var frontFill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var topFill = new SKPaint { Color = BackgroundColor.WithAlpha(210), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var sideFill = new SKPaint { Color = BackgroundColor.WithAlpha(170), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };

            using var top = topBuilder.Detach();
            canvas.DrawPath(top, topFill);
            canvas.DrawPath(top, stroke);
            using var side = sideBuilder.Detach();
            canvas.DrawPath(side, sideFill);
            canvas.DrawPath(side, stroke);
            canvas.DrawRect(front, frontFill);
            canvas.DrawRect(front, stroke);

            using var stereotypeFont = new SKFont(SKTypeface.Default, 9);
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

            float centerX = front.MidX;
            canvas.DrawText($"«{_nodeType.ToLowerInvariant()}»", centerX, Y + 34f, SKTextAlign.Center, stereotypeFont, textPaint);
            canvas.DrawText(_nodeName, centerX, Y + 56f, SKTextAlign.Center, nameFont, textPaint);

            DrawConnectionPoints(canvas, context);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["NodeName"] = _nodeName;
            props["NodeType"] = _nodeType;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("NodeName", out var n) && n is string sn) NodeName = sn;
            if (properties.TryGetValue("NodeType", out var t) && t is string st) NodeType = st;
        }
    }
}