using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Issue node: tracks active problems and blockers in the project.
    /// Red alert style with priority indication.
    /// </summary>
    public class IssueNode : PMControl
    {
        private string _issueTitle = "Issue";
        public string IssueTitle
        {
            get => _issueTitle;
            set
            {
                var v = value ?? "";
                if (_issueTitle != v)
                {
                    _issueTitle = v;
                    if (NodeProperties.TryGetValue("IssueTitle", out var p))
                        p.ParameterCurrentValue = _issueTitle;
                    InvalidateVisual();
                }
            }
        }

        private string _priority = "Medium";
        public new string Priority
        {
            get => _priority;
            set
            {
                var v = value ?? "Medium";
                if (_priority != v)
                {
                    _priority = v;
                    if (NodeProperties.TryGetValue("Priority", out var p))
                        p.ParameterCurrentValue = _priority;
                    InvalidateVisual();
                }
            }
        }

        private string _status = "Open";
        public string Status
        {
            get => _status;
            set
            {
                var v = value ?? "Open";
                if (_status != v)
                {
                    _status = v;
                    if (NodeProperties.TryGetValue("Status", out var p))
                        p.ParameterCurrentValue = _status;
                    InvalidateVisual();
                }
            }
        }

        private string _assignedTo = "";
        public string AssignedTo
        {
            get => _assignedTo;
            set
            {
                var v = value ?? "";
                if (_assignedTo != v)
                {
                    _assignedTo = v;
                    if (NodeProperties.TryGetValue("AssignedTo", out var p))
                        p.ParameterCurrentValue = _assignedTo;
                    InvalidateVisual();
                }
            }
        }

        public IssueNode()
        {
            Name = "PM Issue";
            Width = 150;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["IssueTitle"] = new ParameterInfo
            {
                ParameterName = "IssueTitle",
                ParameterType = typeof(string),
                DefaultParameterValue = _issueTitle,
                ParameterCurrentValue = _issueTitle,
                Description = "Issue title/description"
            };
            NodeProperties["Priority"] = new ParameterInfo
            {
                ParameterName = "Priority",
                ParameterType = typeof(string),
                DefaultParameterValue = _priority,
                ParameterCurrentValue = _priority,
                Description = "Issue priority level",
                Choices = new[] { "Low", "Medium", "High", "Critical" }
            };
            NodeProperties["Status"] = new ParameterInfo
            {
                ParameterName = "Status",
                ParameterType = typeof(string),
                DefaultParameterValue = _status,
                ParameterCurrentValue = _status,
                Description = "Current status",
                Choices = new[] { "Open", "In Progress", "Resolved", "Closed", "Blocked" }
            };
            NodeProperties["AssignedTo"] = new ParameterInfo
            {
                ParameterName = "AssignedTo",
                ParameterType = typeof(string),
                DefaultParameterValue = _assignedTo,
                ParameterCurrentValue = _assignedTo,
                Description = "Person assigned to resolve"
            };
        }

        private SKColor GetPriorityColor()
        {
            return Priority switch
            {
                "Critical" => new SKColor(0xD3, 0x2F, 0x2F), // Dark red
                "High" => new SKColor(0xE5, 0x39, 0x35),     // Red
                "Medium" => new SKColor(0xFF, 0x98, 0x00),   // Orange
                _ => new SKColor(0x75, 0x75, 0x75)           // Gray
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
            SKColor priorityColor = GetPriorityColor();
            bool isResolved = Status == "Resolved" || Status == "Closed";

            using var fill = new SKPaint { Color = priorityColor.WithAlpha(30), IsAntialias = true };
            using var stroke = new SKPaint { Color = priorityColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            // Draw rounded rectangle with colored border
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Draw priority stripe on left edge
            var stripeRect = new SKRect(r.Left, r.Top, r.Left + 6, r.Bottom);
            using var stripeFill = new SKPaint { Color = priorityColor, IsAntialias = true };
            using var stripePath = new SKPath();
            stripePath.MoveTo(r.Left, r.Top + CornerRadius);
            stripePath.LineTo(r.Left, r.Bottom - CornerRadius);
            stripePath.LineTo(r.Left + 6, r.Bottom - CornerRadius);
            stripePath.LineTo(r.Left + 6, r.Top + CornerRadius);
            stripePath.Close();
            canvas.DrawPath(stripePath, stripeFill);

            // Draw alert icon (!)
            float iconX = r.Left + 18;
            float iconY = r.Top + 18;
            using var iconPaint = new SKPaint { Color = priorityColor, IsAntialias = true, StrokeWidth = 2.5f, StrokeCap = SKStrokeCap.Round };
            canvas.DrawLine(iconX, iconY, iconX, iconY + 12, iconPaint);
            canvas.DrawCircle(iconX, iconY + 18, 2f, iconPaint);

            // Draw issue title
            using var titleFont = new SKFont(SKTypeface.Default, 12);
            canvas.DrawText(IssueTitle, r.Left + 32, r.Top + 20, SKTextAlign.Left, titleFont, text);

            // Draw priority
            using var detailFont = new SKFont(SKTypeface.Default, 9);
            using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
            canvas.DrawText($"Priority: {Priority}", r.Left + 12, r.Top + 40, SKTextAlign.Left, detailFont, grayText);

            // Draw status badge
            float badgeY = r.Bottom - 16;
            SKColor statusColor = isResolved ? new SKColor(0x43, 0xA0, 0x47) : new SKColor(0x75, 0x75, 0x75);
            using var badgeFill = new SKPaint { Color = statusColor, IsAntialias = true };
            using var badgeText = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var badgeFont = new SKFont(SKTypeface.Default, 8);

            float badgeWidth = badgeFont.MeasureText(Status, badgeText) + 8;
            var badgeRect = new SKRect(r.Left + 12, badgeY, r.Left + 12 + badgeWidth, badgeY + 12);
            canvas.DrawRoundRect(badgeRect, 3f, 3f, badgeFill);
            canvas.DrawText(Status, badgeRect.Left + 4, badgeRect.Bottom - 2, SKTextAlign.Left, badgeFont, badgeText);

            // Draw assigned indicator if assigned
            if (!string.IsNullOrWhiteSpace(AssignedTo))
            {
                using var assignFont = new SKFont(SKTypeface.Default, 8);
                string assignText = $"→ {AssignedTo}";
                canvas.DrawText(assignText, r.Left + 18 + badgeWidth, badgeY + 10, SKTextAlign.Left, assignFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
