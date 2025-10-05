using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Phase node: represents a major project phase containing multiple tasks.
    /// Larger container with distinct visual style.
    /// </summary>
    public class PhaseNode : PMControl
    {
        private string _phaseName = "Phase";
        public string PhaseName
        {
            get => _phaseName;
            set
            {
                var v = value ?? "";
                if (_phaseName != v)
                {
                    _phaseName = v;
                    if (NodeProperties.TryGetValue("PhaseName", out var p))
                        p.ParameterCurrentValue = _phaseName;
                    InvalidateVisual();
                }
            }
        }

        private string _startDate = "";
        public string StartDate
        {
            get => _startDate;
            set
            {
                var v = value ?? "";
                if (_startDate != v)
                {
                    _startDate = v;
                    if (NodeProperties.TryGetValue("StartDate", out var p))
                        p.ParameterCurrentValue = _startDate;
                    InvalidateVisual();
                }
            }
        }

        private string _endDate = "";
        public string EndDate
        {
            get => _endDate;
            set
            {
                var v = value ?? "";
                if (_endDate != v)
                {
                    _endDate = v;
                    if (NodeProperties.TryGetValue("EndDate", out var p))
                        p.ParameterCurrentValue = _endDate;
                    InvalidateVisual();
                }
            }
        }

        private string _status = "Not Started";
        public string Status
        {
            get => _status;
            set
            {
                var v = value ?? "Not Started";
                if (_status != v)
                {
                    _status = v;
                    if (NodeProperties.TryGetValue("Status", out var p))
                        p.ParameterCurrentValue = _status;
                    InvalidateVisual();
                }
            }
        }

        public PhaseNode()
        {
            Name = "PM Phase";
            Width = 220;
            Height = 90;
            EnsurePortCounts(1, 1);

            NodeProperties["PhaseName"] = new ParameterInfo
            {
                ParameterName = "PhaseName",
                ParameterType = typeof(string),
                DefaultParameterValue = _phaseName,
                ParameterCurrentValue = _phaseName,
                Description = "Phase name"
            };
            NodeProperties["StartDate"] = new ParameterInfo
            {
                ParameterName = "StartDate",
                ParameterType = typeof(string),
                DefaultParameterValue = _startDate,
                ParameterCurrentValue = _startDate,
                Description = "Phase start date"
            };
            NodeProperties["EndDate"] = new ParameterInfo
            {
                ParameterName = "EndDate",
                ParameterType = typeof(string),
                DefaultParameterValue = _endDate,
                ParameterCurrentValue = _endDate,
                Description = "Phase end date"
            };
            NodeProperties["Status"] = new ParameterInfo
            {
                ParameterName = "Status",
                ParameterType = typeof(string),
                DefaultParameterValue = _status,
                ParameterCurrentValue = _status,
                Description = "Phase status",
                Choices = new[] { "Not Started", "In Progress", "Completed", "On Hold", "Cancelled" }
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 30f, bottomInset: 8f);
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;

            // Phase uses double border for emphasis
            using var fill = new SKPaint { Color = MaterialColors.PrimaryContainer, IsAntialias = true };
            using var outerStroke = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            using var innerStroke = new SKPaint { Color = MaterialColors.OnPrimaryContainer, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var text = new SKPaint { Color = MaterialColors.OnPrimaryContainer, IsAntialias = true };

            // Draw outer rectangle
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, outerStroke);

            // Draw inner rectangle
            var innerRect = new SKRect(r.Left + 3, r.Top + 3, r.Right - 3, r.Bottom - 3);
            canvas.DrawRoundRect(innerRect, CornerRadius - 2, CornerRadius - 2, innerStroke);

            // Draw phase name (bold)
            using var nameFont = new SKFont(SKTypeface.Default, 15) { Embolden = true };
            canvas.DrawText(PhaseName, r.Left + 12, r.Top + 22, SKTextAlign.Left, nameFont, text);

            // Draw dates if provided
            if (!string.IsNullOrWhiteSpace(StartDate) || !string.IsNullOrWhiteSpace(EndDate))
            {
                using var dateFont = new SKFont(SKTypeface.Default, 10);
                using var dateText = new SKPaint { Color = MaterialColors.OnPrimaryContainer.WithAlpha(180), IsAntialias = true };
                string dates = $"{StartDate} - {EndDate}";
                canvas.DrawText(dates, r.Left + 12, r.Top + 40, SKTextAlign.Left, dateFont, dateText);
            }

            // Draw status badge
            if (!string.IsNullOrWhiteSpace(Status))
            {
                using var statusFont = new SKFont(SKTypeface.Default, 9);
                using var statusText = new SKPaint { Color = SKColors.White, IsAntialias = true };
                
                SKColor badgeColor = Status switch
                {
                    "Completed" => new SKColor(0x43, 0xA0, 0x47),
                    "In Progress" => new SKColor(0x21, 0x96, 0xF3),
                    "On Hold" => new SKColor(0xFF, 0x98, 0x00),
                    "Cancelled" => new SKColor(0xE5, 0x39, 0x35),
                    _ => new SKColor(0x75, 0x75, 0x75)
                };

                float badgeWidth = statusFont.MeasureText(Status, statusText) + 12;
                float badgeHeight = 16f;
                var badgeRect = new SKRect(r.Right - badgeWidth - 8, r.Bottom - badgeHeight - 8, r.Right - 8, r.Bottom - 8);

                using var badgeFill = new SKPaint { Color = badgeColor, IsAntialias = true };
                canvas.DrawRoundRect(badgeRect, 4f, 4f, badgeFill);
                canvas.DrawText(Status, badgeRect.Left + 6, badgeRect.Bottom - 4, SKTextAlign.Left, statusFont, statusText);
            }

            DrawPorts(canvas);
        }
    }
}
