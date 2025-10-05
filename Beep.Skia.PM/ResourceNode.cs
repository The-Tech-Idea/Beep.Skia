using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Resource node: represents a resource (person, equipment, material) assigned to tasks.
    /// Displays resource allocation and availability.
    /// </summary>
    public class ResourceNode : PMControl
    {
        private string _resourceName = "Resource";
        public string ResourceName
        {
            get => _resourceName;
            set
            {
                var v = value ?? "";
                if (_resourceName != v)
                {
                    _resourceName = v;
                    if (NodeProperties.TryGetValue("ResourceName", out var p))
                        p.ParameterCurrentValue = _resourceName;
                    InvalidateVisual();
                }
            }
        }

        private string _resourceType = "Person";
        public string ResourceType
        {
            get => _resourceType;
            set
            {
                var v = value ?? "Person";
                if (_resourceType != v)
                {
                    _resourceType = v;
                    if (NodeProperties.TryGetValue("ResourceType", out var p))
                        p.ParameterCurrentValue = _resourceType;
                    InvalidateVisual();
                }
            }
        }

        private int _allocationPercent = 100;
        public int AllocationPercent
        {
            get => _allocationPercent;
            set
            {
                var v = System.Math.Clamp(value, 0, 200);
                if (_allocationPercent != v)
                {
                    _allocationPercent = v;
                    if (NodeProperties.TryGetValue("AllocationPercent", out var p))
                        p.ParameterCurrentValue = _allocationPercent;
                    InvalidateVisual();
                }
            }
        }

        private decimal _costPerHour = 0;
        public decimal CostPerHour
        {
            get => _costPerHour;
            set
            {
                if (_costPerHour != value)
                {
                    _costPerHour = value;
                    if (NodeProperties.TryGetValue("CostPerHour", out var p))
                        p.ParameterCurrentValue = _costPerHour;
                    InvalidateVisual();
                }
            }
        }

        public ResourceNode()
        {
            Name = "PM Resource";
            Width = 140;
            Height = 80;
            EnsurePortCounts(0, 2);

            NodeProperties["ResourceName"] = new ParameterInfo
            {
                ParameterName = "ResourceName",
                ParameterType = typeof(string),
                DefaultParameterValue = _resourceName,
                ParameterCurrentValue = _resourceName,
                Description = "Resource name or identifier"
            };
            NodeProperties["ResourceType"] = new ParameterInfo
            {
                ParameterName = "ResourceType",
                ParameterType = typeof(string),
                DefaultParameterValue = _resourceType,
                ParameterCurrentValue = _resourceType,
                Description = "Type: Person, Equipment, Material, Budget",
                Choices = new[] { "Person", "Equipment", "Material", "Budget" }
            };
            NodeProperties["AllocationPercent"] = new ParameterInfo
            {
                ParameterName = "AllocationPercent",
                ParameterType = typeof(int),
                DefaultParameterValue = _allocationPercent,
                ParameterCurrentValue = _allocationPercent,
                Description = "Allocation percentage (0-200%, >100 = over-allocated)"
            };
            NodeProperties["CostPerHour"] = new ParameterInfo
            {
                ParameterName = "CostPerHour",
                ParameterType = typeof(decimal),
                DefaultParameterValue = _costPerHour,
                ParameterCurrentValue = _costPerHour,
                Description = "Hourly cost/rate"
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            
            // Determine color based on allocation
            SKColor fillColor = _allocationPercent > 100 
                ? new SKColor(0xFF, 0xE0, 0xE0)  // Red tint for over-allocated
                : new SKColor(0xE8, 0xF5, 0xE9); // Light green

            using var fill = new SKPaint { Color = fillColor, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            // Draw rounded rectangle
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Draw resource icon (person symbol)
            float iconX = r.Left + 12;
            float iconY = r.Top + 15;
            float iconSize = 10f;

            using var iconPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            
            // Simple person icon
            canvas.DrawCircle(iconX, iconY, iconSize / 3, iconPaint); // Head
            canvas.DrawLine(iconX, iconY + iconSize / 3, iconX, iconY + iconSize, iconPaint); // Body
            canvas.DrawLine(iconX - iconSize / 2, iconY + iconSize / 2, iconX + iconSize / 2, iconY + iconSize / 2, iconPaint); // Arms

            // Draw resource name
            using var nameFont = new SKFont(SKTypeface.Default, 13);
            canvas.DrawText(ResourceName, r.Left + 30, r.Top + 20, SKTextAlign.Left, nameFont, text);

            // Draw resource type
            using var typeFont = new SKFont(SKTypeface.Default, 10);
            using var grayText = new SKPaint { Color = new SKColor(0x70, 0x70, 0x70), IsAntialias = true };
            canvas.DrawText(ResourceType, r.Left + 8, r.Top + 38, SKTextAlign.Left, typeFont, grayText);

            // Draw allocation bar
            if (_allocationPercent > 0)
            {
                float barWidth = r.Width - 16;
                float barHeight = 8f;
                float barY = r.Bottom - 18;

                var barRect = new SKRect(r.Left + 8, barY, r.Left + 8 + barWidth, barY + barHeight);
                using var barBg = new SKPaint { Color = new SKColor(0xE0, 0xE0, 0xE0), IsAntialias = true };
                canvas.DrawRoundRect(barRect, 4f, 4f, barBg);

                float fillWidth = barWidth * System.Math.Min(_allocationPercent / 100f, 1f);
                var fillRect = new SKRect(r.Left + 8, barY, r.Left + 8 + fillWidth, barY + barHeight);
                SKColor barColor = _allocationPercent > 100 ? new SKColor(0xE5, 0x39, 0x35) : MaterialColors.Primary;
                using var barFill = new SKPaint { Color = barColor, IsAntialias = true };
                canvas.DrawRoundRect(fillRect, 4f, 4f, barFill);

                // Draw allocation percentage text
                using var percentFont = new SKFont(SKTypeface.Default, 9);
                string percentText = $"{_allocationPercent}%";
                canvas.DrawText(percentText, r.MidX, barY - 2, SKTextAlign.Center, percentFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
