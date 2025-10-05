using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Gantt Bar node: visual timeline representation showing task duration on a timeline.
    /// Horizontal bar with date range and progress indication.
    /// </summary>
    public class GanttBarNode : PMControl
    {
        private string _taskName = "Task";
        public string TaskName
        {
            get => _taskName;
            set
            {
                var v = value ?? "";
                if (_taskName != v)
                {
                    _taskName = v;
                    if (NodeProperties.TryGetValue("TaskName", out var p))
                        p.ParameterCurrentValue = _taskName;
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

        private int _percentComplete = 0;
        public int PercentComplete
        {
            get => _percentComplete;
            set
            {
                var v = System.Math.Clamp(value, 0, 100);
                if (_percentComplete != v)
                {
                    _percentComplete = v;
                    if (NodeProperties.TryGetValue("PercentComplete", out var p))
                        p.ParameterCurrentValue = _percentComplete;
                    InvalidateVisual();
                }
            }
        }

        public GanttBarNode()
        {
            Name = "PM Gantt Bar";
            Width = 200;
            Height = 50;
            EnsurePortCounts(1, 1);

            NodeProperties["TaskName"] = new ParameterInfo
            {
                ParameterName = "TaskName",
                ParameterType = typeof(string),
                DefaultParameterValue = _taskName,
                ParameterCurrentValue = _taskName,
                Description = "Task name"
            };
            NodeProperties["StartDate"] = new ParameterInfo
            {
                ParameterName = "StartDate",
                ParameterType = typeof(string),
                DefaultParameterValue = _startDate,
                ParameterCurrentValue = _startDate,
                Description = "Task start date"
            };
            NodeProperties["EndDate"] = new ParameterInfo
            {
                ParameterName = "EndDate",
                ParameterType = typeof(string),
                DefaultParameterValue = _endDate,
                ParameterCurrentValue = _endDate,
                Description = "Task end date"
            };
            NodeProperties["PercentComplete"] = new ParameterInfo
            {
                ParameterName = "PercentComplete",
                ParameterType = typeof(int),
                DefaultParameterValue = _percentComplete,
                ParameterCurrentValue = _percentComplete,
                Description = "Completion percentage (0-100)"
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port on left edge
            if (InConnectionPoints.Count > 0)
            {
                var pt = InConnectionPoints[0];
                pt.Center = new SKPoint(r.Left, r.MidY);
                pt.Position = new SKPoint(r.Left - PortRadius, r.MidY);
                pt.Bounds = new SKRect(pt.Center.X - PortRadius, pt.Center.Y - PortRadius, pt.Center.X + PortRadius, pt.Center.Y + PortRadius);
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }

            // Output port on right edge
            if (OutConnectionPoints.Count > 0)
            {
                var pt = OutConnectionPoints[0];
                pt.Center = new SKPoint(r.Right, r.MidY);
                pt.Position = new SKPoint(r.Right + PortRadius, r.MidY);
                pt.Bounds = new SKRect(pt.Center.X - PortRadius, pt.Center.Y - PortRadius, pt.Center.X + PortRadius, pt.Center.Y + PortRadius);
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;

            // Background (timeline bar)
            using var bgPaint = new SKPaint { Color = new SKColor(0xE0, 0xE0, 0xE0), IsAntialias = true };
            canvas.DrawRoundRect(r, 4f, 4f, bgPaint);

            // Progress bar (filled portion)
            if (_percentComplete > 0)
            {
                float fillWidth = r.Width * (_percentComplete / 100f);
                var fillRect = new SKRect(r.Left, r.Top, r.Left + fillWidth, r.Bottom);
                using var fillPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
                canvas.DrawRoundRect(fillRect, 4f, 4f, fillPaint);
            }

            // Border
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            canvas.DrawRoundRect(r, 4f, 4f, stroke);

            // Draw task name
            using var nameFont = new SKFont(SKTypeface.Default, 11);
            using var text = new SKPaint { Color = _percentComplete > 50 ? SKColors.White : MaterialColors.OnSurface, IsAntialias = true };
            canvas.DrawText(TaskName, r.Left + 8, r.Top + 16, SKTextAlign.Left, nameFont, text);

            // Draw dates if provided
            if (!string.IsNullOrWhiteSpace(StartDate) || !string.IsNullOrWhiteSpace(EndDate))
            {
                using var dateFont = new SKFont(SKTypeface.Default, 8);
                using var dateText = new SKPaint { Color = _percentComplete > 50 ? new SKColor(0xFF, 0xFF, 0xFF, 200) : new SKColor(0x70, 0x70, 0x70), IsAntialias = true };
                string dates = $"{StartDate} → {EndDate}";
                canvas.DrawText(dates, r.Left + 8, r.Top + 30, SKTextAlign.Left, dateFont, dateText);
            }

            // Draw percentage on right
            using var percentFont = new SKFont(SKTypeface.Default, 10) { Embolden = true };
            using var percentText = new SKPaint { Color = _percentComplete > 50 ? SKColors.White : MaterialColors.OnSurface, IsAntialias = true };
            string percentStr = $"{_percentComplete}%";
            canvas.DrawText(percentStr, r.Right - 8, r.MidY + 4, SKTextAlign.Right, percentFont, percentText);

            DrawPorts(canvas);
        }
    }
}
