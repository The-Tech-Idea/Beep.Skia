using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a system in a business process diagram.
    /// Displayed as a server/system shape.
    /// </summary>
    public class BusinessSystem : BusinessControl
    {
        private string _systemName = "System";
        public string SystemName
        {
            get => _systemName;
            set
            {
                var v = value ?? string.Empty;
                if (_systemName != v)
                {
                    _systemName = v;
                    if (NodeProperties.TryGetValue("SystemName", out var p)) p.ParameterCurrentValue = _systemName; else NodeProperties["SystemName"] = new ParameterInfo { ParameterName = "SystemName", ParameterType = typeof(string), DefaultParameterValue = _systemName, ParameterCurrentValue = _systemName, Description = "System name" };
                    InvalidateVisual();
                }
            }
        }

        public BusinessSystem()
        {
            Width = 100;
            Height = 120;
            Name = "System";
            ComponentType = BusinessComponentType.System;
            // Seed NodeProperties
            NodeProperties["SystemName"] = new ParameterInfo { ParameterName = "SystemName", ParameterType = typeof(string), DefaultParameterValue = _systemName, ParameterCurrentValue = _systemName, Description = "System name" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = BorderThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw server chassis
            var serverRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(serverRect, 6, 6, fillPaint);
            canvas.DrawRoundRect(serverRect, 6, 6, borderPaint);

            // Draw server details
            DrawServerDetails(canvas);
        }

        private void DrawServerDetails(SKCanvas canvas)
        {
            using var detailPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw horizontal dividers for server units
            for (int i = 1; i < 4; i++)
            {
                float dividerY = Y + (Height / 4) * i;
                canvas.DrawLine(X + 5, dividerY, X + Width - 5, dividerY, detailPaint);
            }

            // Draw power indicators (small circles)
            for (int i = 0; i < 3; i++)
            {
                float indicatorY = Y + 15 + (i * Height / 4);
                float indicatorX = X + Width - 15;

                using var indicatorPaint = new SKPaint
                {
                    Color = i == 0 ? MaterialColors.Primary : MaterialColors.OutlineVariant,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                canvas.DrawCircle(indicatorX, indicatorY, 3, indicatorPaint);
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(SystemName))
                return;

            using var font = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 15;

            canvas.DrawText(SystemName, centerX, textY, SKTextAlign.Center, font, paint);
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}