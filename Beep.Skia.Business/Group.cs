using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a group of related process elements.
    /// Displayed as a dashed border rectangle that can contain multiple components.
    /// </summary>
    public class Group : BusinessControl
    {
        public List<BusinessControl> GroupedComponents { get; set; } = new List<BusinessControl>();
        public string GroupName { get; set; } = "Group";
        public SKColor GroupColor { get; set; } = SKColors.LightBlue;
        public GroupType GroupType { get; set; } = GroupType.Process;

        public Group()
        {
            Width = 200;
            Height = 150;
            Name = "Group";
            ComponentType = BusinessComponentType.Task;
            BackgroundColor = SKColors.Transparent;
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            using var borderPaint = new SKPaint
            {
                Color = GroupColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 8, 4 }, 0)
            };

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);

            // Draw group type indicator
            DrawGroupIndicator(canvas);

            // Draw background fill with low opacity
            using var fillPaint = new SKPaint
            {
                Color = GroupColor.WithAlpha(32), // Very transparent
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawRoundRect(rect, 8, 8, fillPaint);
        }

        private void DrawGroupIndicator(SKCanvas canvas)
        {
            using var indicatorPaint = new SKPaint
            {
                Color = GroupColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            float indicatorX = X + 8;
            float indicatorY = Y + 8;
            float size = 12;

            switch (GroupType)
            {
                case GroupType.Process:
                    // Draw process circle
                    canvas.DrawCircle(indicatorX + size / 2, indicatorY + size / 2, size / 2, indicatorPaint);
                    break;

                case GroupType.Data:
                    // Draw data rectangle
                    var dataRect = new SKRect(indicatorX, indicatorY, indicatorX + size, indicatorY + size);
                    canvas.DrawRect(dataRect, indicatorPaint);
                    break;

                case GroupType.Annotation:
                    // Draw annotation bracket
                    canvas.DrawLine(indicatorX, indicatorY, indicatorX + size, indicatorY, indicatorPaint);
                    canvas.DrawLine(indicatorX, indicatorY, indicatorX, indicatorY + size, indicatorPaint);
                    canvas.DrawLine(indicatorX, indicatorY + size, indicatorX + size, indicatorY + size, indicatorPaint);
                    break;
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(GroupName))
                return;

            using var font = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var paint = new SKPaint
            {
                Color = GroupColor,
                IsAntialias = true
            };

            float textX = X + 25; // After the indicator
            float textY = Y + 15;

            canvas.DrawText(GroupName, textX, textY, SKTextAlign.Left, font, paint);

            // Draw component count
            if (GroupedComponents.Count > 0)
            {
                using var countFont = new SKFont(SKTypeface.Default, 8);
                string countText = $"({GroupedComponents.Count})";
                canvas.DrawText(countText, textX, textY + 12, SKTextAlign.Left, countFont, paint);
            }
        }

        /// <summary>
        /// Adds a component to this group.
        /// </summary>
        public void AddComponent(BusinessControl component)
        {
            GroupedComponents.Add(component);
            UpdateBounds();
        }

        /// <summary>
        /// Removes a component from this group.
        /// </summary>
        public void RemoveComponent(BusinessControl component)
        {
            GroupedComponents.Remove(component);
            UpdateBounds();
        }

        /// <summary>
        /// Updates the group bounds to encompass all grouped components.
        /// </summary>
        protected override void UpdateBounds()
        {
            if (GroupedComponents.Count == 0)
                return;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var component in GroupedComponents)
            {
                minX = Math.Min(minX, component.X);
                minY = Math.Min(minY, component.Y);
                maxX = Math.Max(maxX, component.X + component.Width);
                maxY = Math.Max(maxY, component.Y + component.Height);
            }

            // Add padding
            float padding = 20;
            X = minX - padding;
            Y = minY - padding;
            Width = (maxX - minX) + (padding * 2);
            Height = (maxY - minY) + (padding * 2);
        }
    }
}