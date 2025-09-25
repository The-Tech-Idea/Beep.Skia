using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents an action node in a business rule diagram.
    /// Displayed as a lightning bolt in a rounded rectangle.
    /// </summary>
    public class ActionNode : BusinessControl
    {
        public string ActionText { get; set; } = "Action";
        public ActionType ActionType { get; set; } = ActionType.Execute;

        public ActionNode()
        {
            Width = 120;
            Height = 60;
            Name = "Action";
            ComponentType = BusinessComponentType.Task;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var fillPaint = new SKPaint
            {
                Color = IsEnabled ? BackgroundColor : BackgroundColor.WithAlpha(128),
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

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);

            // Draw lightning bolt icon
            DrawLightningBolt(canvas);
        }

        private void DrawLightningBolt(SKCanvas canvas)
        {
            using var iconPaint = new SKPaint
            {
                Color = IsEnabled ? MaterialColors.Tertiary : MaterialColors.OutlineVariant,
                StrokeWidth = 3,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float boltSize = Math.Min(Width, Height) * 0.3f;

            // Draw lightning bolt path
            using var path = new SKPath();
            path.MoveTo(centerX - boltSize * 0.3f, centerY - boltSize * 0.5f);
            path.LineTo(centerX + boltSize * 0.1f, centerY - boltSize * 0.5f);
            path.LineTo(centerX - boltSize * 0.4f, centerY + boltSize * 0.2f);
            path.LineTo(centerX + boltSize * 0.4f, centerY + boltSize * 0.2f);
            path.LineTo(centerX - boltSize * 0.1f, centerY + boltSize * 0.5f);
            path.LineTo(centerX + boltSize * 0.3f, centerY + boltSize * 0.5f);

            canvas.DrawPath(path, iconPaint);
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(ActionText))
                return;

            using var font = new SKFont(SKTypeface.Default, 9);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float textY = Y + Height + 12;

            canvas.DrawText(ActionText, centerX, textY, SKTextAlign.Center, font, paint);
        }

        protected override void LayoutPorts()
        {
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}