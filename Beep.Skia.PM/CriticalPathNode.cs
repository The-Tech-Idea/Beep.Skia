using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Critical Path node: indicates tasks on the critical path (longest sequence determining project duration).
    /// Diamond shape with warning color.
    /// </summary>
    public class CriticalPathNode : PMControl
    {
        private string _taskName = "Critical Task";
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

        private float _slack = 0f;
        public float Slack
        {
            get => _slack;
            set
            {
                if (_slack != value)
                {
                    _slack = value;
                    if (NodeProperties.TryGetValue("Slack", out var p))
                        p.ParameterCurrentValue = _slack;
                    InvalidateVisual();
                }
            }
        }

        private int _duration = 1;
        public int Duration
        {
            get => _duration;
            set
            {
                var v = System.Math.Max(1, value);
                if (_duration != v)
                {
                    _duration = v;
                    if (NodeProperties.TryGetValue("Duration", out var p))
                        p.ParameterCurrentValue = _duration;
                    InvalidateVisual();
                }
            }
        }

        public CriticalPathNode()
        {
            Name = "PM Critical Path";
            Width = 140;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["TaskName"] = new ParameterInfo
            {
                ParameterName = "TaskName",
                ParameterType = typeof(string),
                DefaultParameterValue = _taskName,
                ParameterCurrentValue = _taskName,
                Description = "Task name"
            };
            NodeProperties["Slack"] = new ParameterInfo
            {
                ParameterName = "Slack",
                ParameterType = typeof(float),
                DefaultParameterValue = _slack,
                ParameterCurrentValue = _slack,
                Description = "Slack time (0 = critical path)"
            };
            NodeProperties["Duration"] = new ParameterInfo
            {
                ParameterName = "Duration",
                ParameterType = typeof(int),
                DefaultParameterValue = _duration,
                ParameterCurrentValue = _duration,
                Description = "Task duration in days"
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port on left point
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

            // Output port on right point
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

            // Diamond shape
            using var path = new SKPath();
            path.MoveTo(r.Left, r.MidY);
            path.LineTo(r.MidX, r.Top);
            path.LineTo(r.Right, r.MidY);
            path.LineTo(r.MidX, r.Bottom);
            path.Close();

            // Critical path = red/orange, has slack = yellow
            SKColor criticalColor = _slack <= 0 ? new SKColor(0xE5, 0x39, 0x35) : new SKColor(0xFF, 0xEB, 0x3B);

            using var fill = new SKPaint { Color = criticalColor.WithAlpha(60), IsAntialias = true };
            using var stroke = new SKPaint { Color = criticalColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw critical path icon (clock/timer)
            float iconX = r.MidX - 8;
            float iconY = r.Top + 14;
            using var iconPaint = new SKPaint { Color = criticalColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            canvas.DrawCircle(iconX, iconY, 6f, iconPaint);
            canvas.DrawLine(iconX, iconY, iconX, iconY - 4, iconPaint); // Hour hand
            canvas.DrawLine(iconX, iconY, iconX + 3, iconY, iconPaint);  // Minute hand

            // Draw task name
            using var nameFont = new SKFont(SKTypeface.Default, 11);
            canvas.DrawText(TaskName, r.MidX, r.MidY + 4, SKTextAlign.Center, nameFont, text);

            // Draw duration and slack
            using var detailFont = new SKFont(SKTypeface.Default, 9);
            using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
            string details = _slack <= 0 ? $"{_duration}d (CRITICAL)" : $"{_duration}d (Slack: {_slack}d)";
            canvas.DrawText(details, r.MidX, r.Bottom - 8, SKTextAlign.Center, detailFont, grayText);

            DrawPorts(canvas);
        }
    }
}
