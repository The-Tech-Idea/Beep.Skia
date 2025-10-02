using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a rule flow connector in a business rule diagram.
    /// Displayed as a curved arrow with rule evaluation indicators.
    /// </summary>
    public class RuleFlow : BusinessControl
    {
        private string _flowLabel = "";
        private FlowDirection _direction = FlowDirection.True;
        private bool _isActive = false;
        public string FlowLabel
        {
            get => _flowLabel;
            set
            {
                var v = value ?? string.Empty;
                if (_flowLabel != v)
                {
                    _flowLabel = v;
                    if (NodeProperties.TryGetValue("FlowLabel", out var p)) p.ParameterCurrentValue = _flowLabel; else NodeProperties["FlowLabel"] = new ParameterInfo { ParameterName = "FlowLabel", ParameterType = typeof(string), DefaultParameterValue = _flowLabel, ParameterCurrentValue = _flowLabel, Description = "Flow label" };
                    InvalidateVisual();
                }
            }
        }
        public FlowDirection Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    if (NodeProperties.TryGetValue("Direction", out var p)) p.ParameterCurrentValue = _direction; else NodeProperties["Direction"] = new ParameterInfo { ParameterName = "Direction", ParameterType = typeof(FlowDirection), DefaultParameterValue = _direction, ParameterCurrentValue = _direction, Description = "Flow direction", Choices = Enum.GetNames(typeof(FlowDirection)) };
                    InvalidateVisual();
                }
            }
        }
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    if (NodeProperties.TryGetValue("IsActive", out var p)) p.ParameterCurrentValue = _isActive; else NodeProperties["IsActive"] = new ParameterInfo { ParameterName = "IsActive", ParameterType = typeof(bool), DefaultParameterValue = _isActive, ParameterCurrentValue = _isActive, Description = "Active state" };
                    InvalidateVisual();
                }
            }
        }

        public RuleFlow()
        {
            Width = 80;
            Height = 40;
            Name = "Rule Flow";
            ComponentType = BusinessComponentType.Task;
            NodeProperties["FlowLabel"] = new ParameterInfo { ParameterName = "FlowLabel", ParameterType = typeof(string), DefaultParameterValue = _flowLabel, ParameterCurrentValue = _flowLabel, Description = "Flow label" };
            NodeProperties["Direction"] = new ParameterInfo { ParameterName = "Direction", ParameterType = typeof(FlowDirection), DefaultParameterValue = _direction, ParameterCurrentValue = _direction, Description = "Flow direction", Choices = Enum.GetNames(typeof(FlowDirection)) };
            NodeProperties["IsActive"] = new ParameterInfo { ParameterName = "IsActive", ParameterType = typeof(bool), DefaultParameterValue = _isActive, ParameterCurrentValue = _isActive, Description = "Active state" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            // Rule flows are typically connectors, so we draw a curved arrow
            DrawCurvedArrow(canvas);
        }

        private void DrawCurvedArrow(SKCanvas canvas)
        {
            using var arrowPaint = new SKPaint
            {
                Color = IsActive ? MaterialColors.Primary : BorderColor,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float startX = X;
            float startY = Y + Height / 2;
            float endX = X + Width;
            float endY = Y + Height / 2;
            float controlY = Direction == FlowDirection.True ?
                Y - Height * 0.5f : Y + Height * 1.5f;

            // Draw curved line
            using var path = new SKPath();
            path.MoveTo(startX, startY);
            path.QuadTo((startX + endX) / 2, controlY, endX, endY);
            canvas.DrawPath(path, arrowPaint);

            // Draw arrowhead
            DrawArrowhead(canvas, endX, endY, arrowPaint);
        }

        private void DrawArrowhead(SKCanvas canvas, float x, float y, SKPaint paint)
        {
            float arrowSize = 8;
            using var arrowPath = new SKPath();

            // Calculate arrow direction based on flow
            float angle = Direction == FlowDirection.True ? -45 : 45;
            float radian = angle * (float)Math.PI / 180f;

            arrowPath.MoveTo(x, y);
            arrowPath.LineTo(
                x - arrowSize * (float)Math.Cos(radian - Math.PI / 6),
                y - arrowSize * (float)Math.Sin(radian - Math.PI / 6)
            );
            arrowPath.LineTo(
                x - arrowSize * (float)Math.Cos(radian + Math.PI / 6),
                y - arrowSize * (float)Math.Sin(radian + Math.PI / 6)
            );
            arrowPath.Close();

            using var fillPaint = new SKPaint
            {
                Color = paint.Color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawPath(arrowPath, fillPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(FlowLabel))
                return;

            using var font = new SKFont(SKTypeface.Default, 8);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Direction == FlowDirection.True ?
                Y - 5 : Y + Height + 15;

            canvas.DrawText(FlowLabel, centerX, textY, SKTextAlign.Center, font, paint);
        }

        protected override void LayoutPorts()
        {
            // RuleFlow is a visual connector; no ports by default.
            EnsurePortCounts(0, 0);
        }
    }
}