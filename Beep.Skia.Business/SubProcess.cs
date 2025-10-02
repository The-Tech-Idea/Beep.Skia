using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Business
{
    /// <summary>
    /// Represents a subprocess that contains other process elements.
    /// Displayed as a rounded rectangle with collapse/expand indicator.
    /// </summary>
    public class SubProcess : BusinessControl
    {
        public List<BusinessControl> ChildComponents { get; set; } = new List<BusinessControl>();
        private bool _isCollapsed = false;
        private string _processName = "Sub Process";
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed != value)
                {
                    _isCollapsed = value;
                    if (NodeProperties.TryGetValue("IsCollapsed", out var p)) p.ParameterCurrentValue = _isCollapsed; else NodeProperties["IsCollapsed"] = new ParameterInfo { ParameterName = "IsCollapsed", ParameterType = typeof(bool), DefaultParameterValue = _isCollapsed, ParameterCurrentValue = _isCollapsed, Description = "Collapsed state" };
                    InvalidateVisual();
                }
            }
        }
        public string ProcessName
        {
            get => _processName;
            set
            {
                var v = value ?? string.Empty;
                if (_processName != v)
                {
                    _processName = v;
                    if (NodeProperties.TryGetValue("ProcessName", out var p)) p.ParameterCurrentValue = _processName; else NodeProperties["ProcessName"] = new ParameterInfo { ParameterName = "ProcessName", ParameterType = typeof(string), DefaultParameterValue = _processName, ParameterCurrentValue = _processName, Description = "Process name" };
                    // Keep Name roughly aligned
                    Name = _processName;
                    InvalidateVisual();
                }
            }
        }
        public int ChildCount => ChildComponents.Count;

        public SubProcess()
        {
            Width = 160;
            Height = 100;
            Name = "Sub Process";
            ComponentType = BusinessComponentType.Task;
            // Seed NodeProperties
            NodeProperties["IsCollapsed"] = new ParameterInfo { ParameterName = "IsCollapsed", ParameterType = typeof(bool), DefaultParameterValue = _isCollapsed, ParameterCurrentValue = _isCollapsed, Description = "Collapsed state" };
            NodeProperties["ProcessName"] = new ParameterInfo { ParameterName = "ProcessName", ParameterType = typeof(string), DefaultParameterValue = _processName, ParameterCurrentValue = _processName, Description = "Process name" };
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

            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 15, 15, fillPaint);
            canvas.DrawRoundRect(rect, 15, 15, borderPaint);

            // Draw subprocess indicator (plus/minus icon)
            DrawCollapseIndicator(canvas);

            // Draw child count if expanded
            if (!IsCollapsed && ChildCount > 0)
            {
                DrawChildIndicators(canvas);
            }
        }

        private void DrawCollapseIndicator(SKCanvas canvas)
        {
            using var indicatorPaint = new SKPaint
            {
                Color = BorderColor,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float indicatorX = X + Width - 20;
            float indicatorY = Y + 10;
            float size = 8;

            // Horizontal line (always visible)
            canvas.DrawLine(indicatorX - size, indicatorY, indicatorX + size, indicatorY, indicatorPaint);

            // Vertical line (only when collapsed)
            if (IsCollapsed)
            {
                canvas.DrawLine(indicatorX, indicatorY - size, indicatorX, indicatorY + size, indicatorPaint);
            }
        }

        private void DrawChildIndicators(SKCanvas canvas)
        {
            using var indicatorPaint = new SKPaint
            {
                Color = MaterialColors.OutlineVariant,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Draw small rectangles representing child components
            int maxIndicators = Math.Min(ChildCount, 6);
            float startX = X + 10;
            float startY = Y + Height - 15;
            float indicatorSize = 4;

            for (int i = 0; i < maxIndicators; i++)
            {
                float indicatorX = startX + (i * (indicatorSize + 2));
                var indicatorRect = new SKRect(
                    indicatorX, startY,
                    indicatorX + indicatorSize, startY + indicatorSize);
                canvas.DrawRect(indicatorRect, indicatorPaint);
            }

            // Draw "..." if there are more children
            if (ChildCount > maxIndicators)
            {
                using var textPaint = new SKPaint
                {
                    Color = MaterialColors.OutlineVariant,
                    IsAntialias = true
                };
                using var font = new SKFont(SKTypeface.Default, 6);

                float dotsX = startX + (maxIndicators * (indicatorSize + 2));
                canvas.DrawText("...", dotsX, startY + indicatorSize, SKTextAlign.Left, font, textPaint);
            }
        }

        protected override void DrawComponentText(SKCanvas canvas)
        {
            using var nameFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var countFont = new SKFont(SKTypeface.Default, 8);
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            float centerX = X + Width / 2;
            float nameY = Y + Height + 15;
            float countY = nameY + 12;

            canvas.DrawText(ProcessName, centerX, nameY, SKTextAlign.Center, nameFont, paint);

            if (ChildCount > 0)
            {
                string countText = IsCollapsed ? $"{ChildCount} items" : $"{ChildCount} children";
                canvas.DrawText(countText, centerX, countY, SKTextAlign.Center, countFont, paint);
            }
        }

        /// <summary>
        /// Adds a child component to this subprocess.
        /// </summary>
        public void AddChild(BusinessControl component)
        {
            ChildComponents.Add(component);
            if (!string.IsNullOrEmpty(component.Name))
                ChildNodes[component.Name] = component;
        }

        /// <summary>
        /// Removes a child component from this subprocess.
        /// </summary>
        public void RemoveChild(BusinessControl component)
        {
            ChildComponents.Remove(component);
            if (!string.IsNullOrEmpty(component.Name))
                ChildNodes.Remove(component.Name);
        }

        /// <summary>
        /// Toggles the collapsed/expanded state of the subprocess.
        /// </summary>
        public void ToggleCollapsed()
        {
            IsCollapsed = !IsCollapsed;
        }

        protected override void LayoutPorts()
        {
            // Rounded rectangle: 1 in / 1 out
            EnsurePortCounts(1, 1);
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }
    }
}